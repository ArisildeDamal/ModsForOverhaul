using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf
{
	public class ModelIcosahedronUtil
	{
		public static MeshData genIcosahedron(int depth, float radius)
		{
			MeshData modeldata = new MeshData(10, 10, false, true, true, true);
			int index = 0;
			for (int i = 0; i < ModelIcosahedronUtil.tindx.Length; i++)
			{
				ModelIcosahedronUtil.subdivide(modeldata, ref index, ModelIcosahedronUtil.vdata[ModelIcosahedronUtil.tindx[i][0]], ModelIcosahedronUtil.vdata[ModelIcosahedronUtil.tindx[i][1]], ModelIcosahedronUtil.vdata[ModelIcosahedronUtil.tindx[i][2]], depth, radius);
			}
			return modeldata;
		}

		private static void subdivide(MeshData modeldata, ref int index, double[] vA0, double[] vB1, double[] vC2, int depth, float radius)
		{
			double[] vAB = new double[3];
			double[] vBC = new double[3];
			double[] vCA = new double[3];
			if (depth == 0)
			{
				ModelIcosahedronUtil.addTriangle(modeldata, ref index, vA0, vB1, vC2, radius);
				return;
			}
			for (int i = 0; i < 3; i++)
			{
				vAB[i] = (vA0[i] + vB1[i]) / 2.0;
				vBC[i] = (vB1[i] + vC2[i]) / 2.0;
				vCA[i] = (vC2[i] + vA0[i]) / 2.0;
			}
			double modAB = ModelIcosahedronUtil.mod(vAB);
			double modBC = ModelIcosahedronUtil.mod(vBC);
			double modCA = ModelIcosahedronUtil.mod(vCA);
			for (int i = 0; i < 3; i++)
			{
				vAB[i] /= modAB;
				vBC[i] /= modBC;
				vCA[i] /= modCA;
			}
			ModelIcosahedronUtil.subdivide(modeldata, ref index, vA0, vAB, vCA, depth - 1, radius);
			ModelIcosahedronUtil.subdivide(modeldata, ref index, vB1, vBC, vAB, depth - 1, radius);
			ModelIcosahedronUtil.subdivide(modeldata, ref index, vC2, vCA, vBC, depth - 1, radius);
			ModelIcosahedronUtil.subdivide(modeldata, ref index, vAB, vBC, vCA, depth - 1, radius);
		}

		public static double mod(double[] v)
		{
			return Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
		}

		private static double[] calcTextureMap(double[] vtx)
		{
			double[] ret = new double[3];
			ret[0] = Math.Sqrt(vtx[0] * vtx[0] + vtx[1] * vtx[1] + vtx[2] * vtx[2]);
			ret[1] = Math.Acos(vtx[2] / ret[0]);
			ret[2] = Math.Atan2(vtx[1], vtx[0]);
			ret[1] += 3.141592653589793;
			ret[1] /= 6.283185307179586;
			ret[2] += 3.141592653589793;
			ret[2] /= 6.283185307179586;
			return ret;
		}

		private static void addTriangle(MeshData modeldata, ref int index, double[] v1, double[] v2, double[] v3, float radius)
		{
			double[] spherical = ModelIcosahedronUtil.calcTextureMap(v1);
			modeldata.AddVertex((float)((double)radius * v1[0]), (float)((double)radius * v1[1]), (float)((double)radius * v1[2]), (float)spherical[1], (float)spherical[2], (int)ModelIcosahedronUtil.white);
			int num = index;
			index = num + 1;
			modeldata.AddIndex(num);
			spherical = ModelIcosahedronUtil.calcTextureMap(v2);
			modeldata.AddVertex((float)((double)radius * v2[0]), (float)((double)radius * v2[1]), (float)((double)radius * v2[2]), (float)spherical[1], (float)spherical[2], (int)ModelIcosahedronUtil.white);
			num = index;
			index = num + 1;
			modeldata.AddIndex(num);
			spherical = ModelIcosahedronUtil.calcTextureMap(v3);
			modeldata.AddVertex((float)((double)radius * v3[0]), (float)((double)radius * v3[1]), (float)((double)radius * v3[2]), (float)spherical[1], (float)spherical[2], (int)ModelIcosahedronUtil.white);
			num = index;
			index = num + 1;
			modeldata.AddIndex(num);
		}

		// Note: this type is marked as 'beforefieldinit'.
		static ModelIcosahedronUtil()
		{
			double[][] array = new double[12][];
			array[0] = new double[]
			{
				-ModelIcosahedronUtil.X,
				0.0,
				ModelIcosahedronUtil.Z
			};
			array[1] = new double[]
			{
				ModelIcosahedronUtil.X,
				0.0,
				ModelIcosahedronUtil.Z
			};
			array[2] = new double[]
			{
				-ModelIcosahedronUtil.X,
				0.0,
				-ModelIcosahedronUtil.Z
			};
			array[3] = new double[]
			{
				ModelIcosahedronUtil.X,
				0.0,
				-ModelIcosahedronUtil.Z
			};
			array[4] = new double[]
			{
				0.0,
				ModelIcosahedronUtil.Z,
				ModelIcosahedronUtil.X
			};
			array[5] = new double[]
			{
				0.0,
				ModelIcosahedronUtil.Z,
				-ModelIcosahedronUtil.X
			};
			array[6] = new double[]
			{
				0.0,
				-ModelIcosahedronUtil.Z,
				ModelIcosahedronUtil.X
			};
			array[7] = new double[]
			{
				0.0,
				-ModelIcosahedronUtil.Z,
				-ModelIcosahedronUtil.X
			};
			int num = 8;
			double[] array2 = new double[3];
			array2[0] = ModelIcosahedronUtil.Z;
			array2[1] = ModelIcosahedronUtil.X;
			array[num] = array2;
			int num2 = 9;
			double[] array3 = new double[3];
			array3[0] = -ModelIcosahedronUtil.Z;
			array3[1] = ModelIcosahedronUtil.X;
			array[num2] = array3;
			int num3 = 10;
			double[] array4 = new double[3];
			array4[0] = ModelIcosahedronUtil.Z;
			array4[1] = -ModelIcosahedronUtil.X;
			array[num3] = array4;
			int num4 = 11;
			double[] array5 = new double[3];
			array5[0] = -ModelIcosahedronUtil.Z;
			array5[1] = -ModelIcosahedronUtil.X;
			array[num4] = array5;
			ModelIcosahedronUtil.vdata = array;
			ModelIcosahedronUtil.tindx = new int[][]
			{
				new int[] { 0, 4, 1 },
				new int[] { 0, 9, 4 },
				new int[] { 9, 5, 4 },
				new int[] { 4, 5, 8 },
				new int[] { 4, 8, 1 },
				new int[] { 8, 10, 1 },
				new int[] { 8, 3, 10 },
				new int[] { 5, 3, 8 },
				new int[] { 5, 2, 3 },
				new int[] { 2, 7, 3 },
				new int[] { 7, 10, 3 },
				new int[] { 7, 6, 10 },
				new int[] { 7, 11, 6 },
				new int[] { 11, 0, 6 },
				new int[] { 0, 1, 6 },
				new int[] { 6, 1, 10 },
				new int[] { 9, 0, 11 },
				new int[] { 9, 11, 2 },
				new int[] { 9, 2, 5 },
				new int[] { 7, 2, 11 }
			};
		}

		public static uint white = uint.MaxValue;

		public static double X = 0.525731086730957;

		public static double Z = 0.8506507873535156;

		public static double[][] vdata;

		public static int[][] tindx;
	}
}
