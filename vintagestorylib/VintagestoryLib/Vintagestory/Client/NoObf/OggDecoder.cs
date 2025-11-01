using System;
using System.IO;
using csogg;
using csvorbis;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf
{
	public class OggDecoder
	{
		public AudioMetaData OggToWav(Stream ogg, IAsset asset)
		{
			AudioMetaData sample = new AudioMetaData(asset);
			sample.Loaded = 1;
			TextWriter s_err = new StringWriter();
			Stream input = null;
			MemoryStream output = null;
			input = ogg;
			output = new MemoryStream();
			SyncState oy = new SyncState();
			StreamState os = new StreamState();
			Page og = new OggPage();
			Packet op = new Packet();
			Info vi = new Info();
			Comment vc = new Comment();
			DspState vd = new DspState();
			global::csvorbis.Block vb = new global::csvorbis.Block(vd);
			int bytes = 0;
			oy.init();
			int eos = 0;
			int index = oy.buffer(4096);
			byte[] buffer = oy.data;
			try
			{
				bytes = input.Read(buffer, index, 4096);
			}
			catch (Exception e)
			{
				s_err.WriteLine(LoggerBase.CleanStackTrace(e.ToString()));
			}
			oy.wrote(bytes);
			if (oy.pageout(og) != 1)
			{
				if (bytes < 4096)
				{
					goto IL_04FD;
				}
				s_err.WriteLine("Input does not appear to be an Ogg bitstream.");
			}
			os.init(og.serialno());
			vi.init();
			vc.init();
			if (os.pagein(og) < 0)
			{
				s_err.WriteLine("Error reading first page of Ogg bitstream data.");
			}
			if (os.packetout(op) != 1)
			{
				s_err.WriteLine("Error reading initial header packet.");
			}
			if (vi.synthesis_headerin(vc, op) < 0)
			{
				s_err.WriteLine("This Ogg bitstream does not contain Vorbis audio data.");
			}
			int i = 0;
			while (i < 2)
			{
				while (i < 2)
				{
					int result = oy.pageout(og);
					if (result == 0)
					{
						break;
					}
					if (result == 1)
					{
						os.pagein(og);
						while (i < 2)
						{
							result = os.packetout(op);
							if (result == 0)
							{
								break;
							}
							if (result == -1)
							{
								s_err.WriteLine("Corrupt secondary header.  Exiting.");
							}
							vi.synthesis_headerin(vc, op);
							i++;
						}
					}
				}
				index = oy.buffer(4096);
				buffer = oy.data;
				try
				{
					bytes = input.Read(buffer, index, 4096);
				}
				catch (Exception e2)
				{
					s_err.WriteLine(LoggerBase.CleanStackTrace(e2.ToString()));
				}
				if (bytes == 0 && i < 2)
				{
					s_err.WriteLine("End of file before finding all Vorbis headers!");
				}
				oy.wrote(bytes);
			}
			byte[][] ptr = vc.user_comments;
			int j = 0;
			while (j < vc.user_comments.Length && ptr[j] != null)
			{
				s_err.WriteLine(vc.getComment(j));
				j++;
			}
			s_err.WriteLine(string.Concat(new string[]
			{
				"\nBitstream is ",
				vi.channels.ToString(),
				" channel, ",
				vi.rate.ToString(),
				"Hz"
			}));
			s_err.WriteLine("Encoded by: " + vc.getVendor() + "\n");
			sample.Channels = vi.channels;
			sample.Rate = vi.rate;
			int convsize = 4096 / vi.channels;
			vd.synthesis_init(vi);
			vb.init(vd);
			float[][][] _pcm = new float[1][][];
			int[] _index = new int[vi.channels];
			if (OggDecoder.convbuffer == null)
			{
				OggDecoder.convbuffer = new byte[8192];
			}
			while (eos == 0)
			{
				while (eos == 0)
				{
					int result2 = oy.pageout(og);
					if (result2 == 0)
					{
						break;
					}
					if (result2 == -1)
					{
						s_err.WriteLine("Corrupt or missing data in bitstream; continuing...");
					}
					else
					{
						os.pagein(og);
						for (;;)
						{
							result2 = os.packetout(op);
							if (result2 == 0)
							{
								break;
							}
							if (result2 != -1)
							{
								if (vb.synthesis(op) == 0)
								{
									vd.synthesis_blockin(vb);
								}
								int samples;
								while ((samples = vd.synthesis_pcmout(_pcm, _index)) > 0)
								{
									float[][] pcm = _pcm[0];
									int bout = ((samples < convsize) ? samples : convsize);
									for (i = 0; i < vi.channels; i++)
									{
										int ptr2 = i * 2;
										int mono = _index[i];
										for (int k = 0; k < bout; k++)
										{
											int val = (int)((double)pcm[i][mono + k] * 32767.0);
											if (val > 32767)
											{
												val = 32767;
											}
											if (val < -32768)
											{
												val = -32768;
											}
											if (val < 0)
											{
												val |= 32768;
											}
											OggDecoder.convbuffer[ptr2] = (byte)val;
											OggDecoder.convbuffer[ptr2 + 1] = (byte)((uint)val >> 8);
											ptr2 += 2 * vi.channels;
										}
									}
									output.Write(OggDecoder.convbuffer, 0, 2 * vi.channels * bout);
									vd.synthesis_read(bout);
								}
							}
						}
						if (og.eos() != 0)
						{
							eos = 1;
						}
					}
				}
				if (eos == 0)
				{
					index = oy.buffer(4096);
					buffer = oy.data;
					try
					{
						bytes = input.Read(buffer, index, 4096);
					}
					catch (Exception e3)
					{
						s_err.WriteLine(LoggerBase.CleanStackTrace(e3.ToString()));
					}
					oy.wrote(bytes);
					if (bytes == 0)
					{
						eos = 1;
					}
				}
			}
			os.clear();
			vb.clear();
			vd.clear();
			vi.clear();
			IL_04FD:
			oy.clear();
			input.Close();
			sample.Pcm = output.ToArray();
			return sample;
		}

		private const int buffersize = 8192;

		[ThreadStatic]
		private static byte[] convbuffer;
	}
}
