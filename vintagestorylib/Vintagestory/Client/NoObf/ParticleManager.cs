using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class ParticleManager
	{
		public void Init(SystemRenderParticles particleSystem)
		{
			this.particleSystem = particleSystem;
			this.blockBreakingProps = new BlockBreakingParticleProps();
			particleSystem.game.eventManager.OnPlayerBreakingBlock.Add(new Action<BlockDamage>(this.SpawnBlockBreakingParticles));
			particleSystem.game.api.eventapi.RegisterAsyncParticleSpawner(new ContinousParticleSpawnTaskDelegate(this.AsyncParticleSpawnTick));
			particleSystem.game.api.eventapi.RegisterGameTickListener(new Action<float>(this.copyLoadedEntitiesList2sec), 2000, 123);
		}

		private void copyLoadedEntitiesList2sec(float dt)
		{
			Dictionary<long, Entity> dict = new Dictionary<long, Entity>();
			foreach (KeyValuePair<long, Entity> val in this.particleSystem.game.LoadedEntities)
			{
				dict[val.Key] = val.Value;
			}
			this.loadedEntities = dict;
		}

		private bool AsyncParticleSpawnTick(float dt, IAsyncParticleManager manager)
		{
			foreach (KeyValuePair<long, Entity> val in this.loadedEntities)
			{
				val.Value.OnAsyncParticleTick(dt, manager);
			}
			if (this.asyncParticleQueue.Count == 0)
			{
				return true;
			}
			object obj = this.asyncParticleQueueLock;
			lock (obj)
			{
				while (this.asyncParticleQueue.Count > 0)
				{
					IParticlePropertiesProvider p = this.asyncParticleQueue.Dequeue();
					this.particleSystem.offthreadpools[(int)p.ParticleModel].SpawnParticles(p);
				}
			}
			return true;
		}

		public void SpawnParticles(float quantity, int color, Vec3d minPos, Vec3d maxPos, Vec3f minVelocity, Vec3f maxVelocity, float lifeLength, float gravityEffect, float scale, EnumParticleModel model)
		{
			SimpleParticleProperties props = new SimpleParticleProperties(quantity, quantity, color, minPos, maxPos, minVelocity, maxVelocity, lifeLength, gravityEffect, 1f, 1f, EnumParticleModel.Cube);
			props.Init(this.particleSystem.game.api);
			props.ParticleModel = model;
			SimpleParticleProperties simpleParticleProperties = props;
			props.MaxSize = scale;
			simpleParticleProperties.MinSize = scale;
			this.particleSystem.mainthreadpools[(int)props.ParticleModel].SpawnParticles(props);
		}

		public void EnqueueAsyncParticles(IParticlePropertiesProvider props)
		{
			object obj = this.asyncParticleQueueLock;
			lock (obj)
			{
				this.asyncParticleQueue.Enqueue(props);
			}
		}

		public void SpawnParticles(IParticlePropertiesProvider properties)
		{
			bool isMainThread = Environment.CurrentManagedThreadId == RuntimeEnv.MainThreadId;
			if (isMainThread && properties.Async)
			{
				this.EnqueueAsyncParticles(properties);
				return;
			}
			properties.Init(this.particleSystem.game.api);
			if (!isMainThread)
			{
				this.particleSystem.offthreadpools[(int)properties.ParticleModel].SpawnParticles(properties);
				return;
			}
			this.particleSystem.mainthreadpools[(int)properties.ParticleModel].SpawnParticles(properties);
		}

		public void SpawnBlockBreakingParticles(BlockDamage blockdamage)
		{
			this.blockBreakingProps.Init(this.particleSystem.game.api);
			this.blockBreakingProps.blockdamage = blockdamage;
			this.blockBreakingProps.boyant = blockdamage.Block.MaterialDensity < 1000;
			this.particleSystem.mainthreadpools[(int)this.blockBreakingProps.ParticleModel].SpawnParticles(this.blockBreakingProps);
		}

		public int SpawnParticlesOffThread(IParticlePropertiesProvider particleProperties)
		{
			return this.particleSystem.offthreadpools[(int)particleProperties.ParticleModel].SpawnParticles(particleProperties);
		}

		public int ParticlesAlive(EnumParticleModel model)
		{
			return this.particleSystem.offthreadpools[(int)model].QuantityAlive;
		}

		protected SystemRenderParticles particleSystem;

		protected BlockBreakingParticleProps blockBreakingProps;

		protected object asyncParticleQueueLock = new object();

		protected Queue<IParticlePropertiesProvider> asyncParticleQueue = new Queue<IParticlePropertiesProvider>();

		private Dictionary<long, Entity> loadedEntities = new Dictionary<long, Entity>();
	}
}
