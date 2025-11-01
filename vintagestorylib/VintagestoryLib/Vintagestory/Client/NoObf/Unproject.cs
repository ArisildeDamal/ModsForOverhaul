using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf
{
	public class Unproject
	{
		public Unproject()
		{
			this.finalMatrix = Mat4d.Create();
			this.inp = new double[4];
			this.out_ = new double[4];
		}

		public bool UnProject(int winX, int winY, int winZ, double[] model, double[] proj, double[] view, double[] objPos)
		{
			this.inp[0] = (double)winX;
			this.inp[1] = (double)winY;
			this.inp[2] = (double)winZ;
			this.inp[3] = 1.0;
			Mat4d.Multiply(this.finalMatrix, proj, model);
			Mat4d.Invert(this.finalMatrix, this.finalMatrix);
			this.inp[0] = (this.inp[0] - view[0]) / view[2];
			this.inp[1] = (this.inp[1] - view[1]) / view[3];
			this.inp[0] = this.inp[0] * 2.0 - 1.0;
			this.inp[1] = this.inp[1] * 2.0 - 1.0;
			this.inp[2] = this.inp[2] * 2.0 - 1.0;
			this.MultMatrixVec(this.finalMatrix, this.inp, this.out_);
			if (this.out_[3] == 0.0)
			{
				return false;
			}
			this.out_[0] /= this.out_[3];
			this.out_[1] /= this.out_[3];
			this.out_[2] /= this.out_[3];
			objPos[0] = this.out_[0];
			objPos[1] = this.out_[1];
			objPos[2] = this.out_[2];
			return true;
		}

		private void MultMatrixVec(double[] matrix, double[] inp__, double[] out__)
		{
			for (int i = 0; i < 4; i++)
			{
				out__[i] = inp__[0] * matrix[i] + inp__[1] * matrix[4 + i] + inp__[2] * matrix[8 + i] + inp__[3] * matrix[12 + i];
			}
		}

		private double[] finalMatrix;

		private double[] inp;

		private double[] out_;
	}
}
