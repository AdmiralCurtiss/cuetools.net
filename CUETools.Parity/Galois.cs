using System;
using System.Collections.Generic;
using System.Text;

namespace CUETools.Parity
{
	public class Galois
	{
		private ushort[] expTbl; // 二重にもつことによりmul, div等を簡略化
		private ushort[] logTbl;
		private int w;
		private int max;
		private int symStart = 0;

		/**
		 * スカラー、ベクターの相互変換テーブルの作成
		 */
		public Galois(int polynomial, int _w)
		{
			w = _w;
			max = (1 << _w) - 1;

			expTbl = new ushort[max * 2];
			logTbl = new ushort[max + 1];

			int d = 1;
			for (int i = 0; i < max; i++)
			{
				//if (d == 0)
				//    throw new Exception("oops");
				expTbl[i] = expTbl[max + i] = (ushort)d;
				logTbl[d] = (ushort)i;
				d <<= 1;
				if (((d >> _w) & 1) != 0)
					d = (d ^ polynomial) & max;
			}
		}

		public int Max
		{
			get
			{
				return max;
			}
		}

		public ushort[] ExpTbl
		{
			get
			{
				return expTbl;
			}
		}

		public ushort[] LogTbl
		{
			get
			{
				return logTbl;
			}
		}

		/**
		 * スカラー -> ベクター変換
		 *
		 * @param a int
		 * @return int
		 */
		public int toExp(int a)
		{
			return expTbl[a];
		}

		/**
		 * ベクター -> スカラー変換
		 *
		 * @param a int
		 * @return int
		 */
		public int toLog(int a)
		{
			return logTbl[a];
		}

		/**
		 * 誤り位置インデックスの計算
		 *
		 * @param length int
		 * 		データ長
		 * @param a int
		 * 		誤り位置ベクター
		 * @return int
		 * 		誤り位置インデックス
		 */
		public int toPos(int length, int a)
		{
			return length - 1 - logTbl[a];
		}

		/**
		 * 掛け算
		 *
		 * @param a int
		 * @param b int
		 * @return int
		 * 		= a * b
		 */
		public int mul(int a, int b)
		{
			return (a == 0 || b == 0) ? 0 : expTbl[(int)logTbl[a] + logTbl[b]];
		}

		/**
		 * 掛け算
		 *
		 * @param a int
		 * @param b int
		 * @return int
		 * 		= a * α^b
		 */
		public int mulExp(int a, int b)
		{
			return (a == 0) ? 0 : expTbl[logTbl[a] + b];
		}

		/**
		 * 割り算
		 *
		 * @param a int
		 * @param b int
		 * @return int
		 * 		= a / b
		 */
		public int div(int a, int b)
		{
			return (a == 0) ? 0 : expTbl[logTbl[a] - logTbl[b] + max];
		}

		/**
		 * 割り算
		 *
		 * @param a int
		 * @param b int
		 * @return int
		 * 		= a / α^b
		 */
		public int divExp(int a, int b)
		{
			return (a == 0) ? 0 : expTbl[logTbl[a] - b + max];
		}

		/**
		 * 逆数
		 *
		 * @param a int
		 * @return int
		 * 		= 1/a
		 */
		public int inv(int a)
		{
			return expTbl[max - logTbl[a]];
		}

		public int[] toLog(int[] a)
		{
			var res = new int[a.Length];
			for (int i = 0; i < a.Length; i++)
				res[i] = a[i] == 0 ? - 1 : toLog(a[i]);
			return res;
		}

		public int[] toLog(ushort[] a)
		{
			var res = new int[a.Length];
			for (int i = 0; i < a.Length; i++)
				res[i] = a[i] == 0 ? -1 : toLog(a[i]);
			return res;
		}

		public int[] toExp(int[] a)
		{
			var res = new int[a.Length];
			for (int i = 0; i < a.Length; i++)
				res[i] = a[i] == -1 ? 0 : toExp(a[i]);
			return res;
		}

		public int gfadd(int a, int b)
		{
			var a_exp = a == -1 ? 0 : toExp(a);
			var b_exp = b == -1 ? 0 : toExp(b);
			var res_exp = a_exp ^ b_exp;
			return res_exp == 0 ? -1 : toLog(res_exp);
		}

		public int[] gfadd(int[] a, int b)
		{
			var res = new int[a.Length];
			var a_exp = toExp(a);
			var b_exp = b == -1 ? 0 : toExp(b);			
			for (int i = 0; i < a.Length; i++)
				res[i] = a_exp[i] ^ b_exp;
			return toLog(res);
		}

		public int[] gfdiff(int[] a)
		{
			//l = length(polynomial);
			//for cc = 2:l
			//        %cc-1 represents the power of x
			//        if mod(cc-1,2) == 0 %all the even powers are zero because of GF(2)
			//            diff(cc-1) = -Inf; 
			//        else
			//            diff(cc-1) = polynomial(cc);
			//        end
			//end		
			var res = new int[a.Length - 1];
			for (int i = 0; i < res.Length; i++)
				res[i] = (i % 2) == 0 ? a[i + 1] : -1;
			return res;
		}

		public int gfmul(int a, int b)
		{
			return a < 0 || b < 0 ? -1 : ((a + b) % max);
		}

		public int gfdiv(int a, int b)
		{
			return a < 0 ? -1 : ((max + a - b) % max);
		}

		public int gfpow(int value, int p)
		{
			return (value * p) % max;
		}

		public int gfsubstitute(int[] polynomial, int value, int terms)
		{
			var sum = 0;
			if (value != -1)
				for (int p = 0; p < terms; p++)
					if (polynomial[p] != -1)
					{
						var pow = polynomial[p] + value * p;
						sum ^= expTbl[(pow & max) + (pow >> w)];
					}
			return sum == 0 ? -1 : logTbl[sum];
		}

		public int[] gfconv(int[] a, int[] b)
		{
			return gfconv(a, b, a.Length + b.Length - 1);
		}

		public int[] gfconv(int[] a, int[] b, int len)
		{
			var seki = new int[len];
			for (int ia = 0; ia < a.Length; ia++)
			{
				var loga = a[ia];
				if (loga != -1)
				{
					int ib2 = Math.Min(b.Length, len - ia);
					for (int ib = 0; ib < ib2; ib++)
					{
						var logb = b[ib];
						if (logb != -1)
							seki[ia + ib] ^= expTbl[loga + logb]; // = a[ia] * b[ib]
					}
				}
			}
			for (int i = 0; i < len; i++)
				seki[i] = seki[i] == 0 ? -1 : logTbl[seki[i]];
			return seki;
		}

		public unsafe void gfconv(int* a, int alen, int* b, int blen, int* c, int clen)
		{
			for (int i = 0; i < clen; i++)
				c[i] = 0;
			for (int ia = 0; ia < alen; ia++)
			{
				var loga = a[ia];
				if (loga != -1)
				{
					int ib2 = Math.Min(blen, clen - ia);
					for (int ib = 0; ib < ib2; ib++)
					{
						var logb = b[ib];
						if (logb != -1)
							c[ia + ib] ^= expTbl[loga + logb]; // = a[ia] * b[ib]
					}
				}
			}
			for (int i = 0; i < clen; i++)
				c[i] = c[i] == 0 ? -1 : logTbl[c[i]];
		}

		public int[] mulPoly(int[] a, int[] b)
		{
			return mulPoly(a, b, a.Length + b.Length - 1);
		}

		public int[] mulPoly(int[] a, int[] b, int len)
		{
			var res = new int[len];
			mulPoly(res, a, b);
			return res;
		}

		/**
		 * 数式の掛け算
		 *
		 * @param seki int[]
		 * 		seki = a * b
		 * @param a int[]
		 * @param b int[]
		 */
		public void mulPoly(int[] seki, int[] a, int[] b)
		{
			Array.Clear(seki, 0, seki.Length);
			for (int ia = 0; ia < a.Length; ia++)
			{
				if (a[ia] != 0)
				{
					int loga = logTbl[a[ia]];
					int ib2 = Math.Min(b.Length, seki.Length - ia);
					for (int ib = 0; ib < ib2; ib++)
					{
						if (b[ib] != 0)
						{
							seki[ia + ib] ^= expTbl[loga + logTbl[b[ib]]];	// = a[ia] * b[ib]
						}
					}
				}
			}
		}

		public unsafe void mulPoly(int* seki, int* a, int* b, int lenS, int lenA, int lenB)
		{
			for (int i = 0; i < lenS; i++)
				seki[i] = 0;
			for (int ia = 0; ia < lenA; ia++)
			{
				if (a[ia] != 0)
				{
					int loga = logTbl[a[ia]];
					int ib2 = Math.Min(lenB, lenS - ia);
					for (int ib = 0; ib < ib2; ib++)
					{
						if (b[ib] != 0)
						{
							seki[ia + ib] ^= expTbl[loga + logTbl[b[ib]]];	// = a[ia] * b[ib]
						}
					}
				}
			}
		}

		/**
		 * 生成多項式配列の作成
		 *		G(x)=Π[k=0,n-1](x + α^k)
		 *		encodeGxの添え字と次数の並びが逆なのに注意
		 *		encodeGx[0]        = x^(npar - 1)の項
		 *		encodeGx[1]        = x^(npar - 2)の項
		 *		...
		 *		encodeGx[npar - 1] = x^0の項
		 */
		public int[] makeEncodeGx(int npar)
		{
			int[] encodeGx = new int[npar];
			encodeGx[npar - 1] = 1;
			for (int i = 0, kou = symStart; i < npar; i++, kou++)
			{
				int ex = toExp(kou); // ex = α^kou
				// (x + α^kou)を掛る
				for (int j = 0; j < npar - 1; j++)
				{
					// 現在の項 * α^kou + 一つ下の次数の項
					encodeGx[j] = mul(encodeGx[j], ex) ^ encodeGx[j + 1];
				}
				encodeGx[npar - 1] = mul(encodeGx[npar - 1], ex);// 最下位項の計算
			}
			return encodeGx;
		}

		public int[] makeEncodeGxLog(int npar)
		{
			int[] encodeGx = makeEncodeGx(npar);
			for (int i = 0; i < npar; i++)
			{
				if (encodeGx[i] == 0)
					throw new Exception("0 in encodeGx");
				encodeGx[i] = toLog(encodeGx[i]);
			}
			return encodeGx;
		}

		/// <summary>
		/// parityTable[xx, 0, i] = mul(00xx, encodeGx[i])
		/// parityTable[xx, 1, i] = mul(xx00, encodeGx[i])
		/// </summary>
		/// <param name="npar"></param>
		/// <returns></returns>
		public ushort[,,] makeEncodeTable(int npar)
		{
			var loggx = makeEncodeGxLog(npar);
			var parityTable = new ushort[256, 2, npar];
			for (int i = 0; i < npar; i++)
			{
				parityTable[0, 0, i] = 0;
				parityTable[0, 1, i] = 0;
			}
			for (int ib = 1; ib < 256; ib++)
			{
				int logib0 = LogTbl[ib];
				int logib1 = LogTbl[ib << 8];
				for (int i = 0; i < npar; i++)
				{
					parityTable[ib, 0, i] = ExpTbl[logib0 + loggx[i]];
					parityTable[ib, 1, i] = ExpTbl[logib1 + loggx[i]];
				}
			}
			return parityTable;
		}

		/// <summary>
		/// parityTable[xx, 0, i] = mul(00xx, α^i)
		/// parityTable[xx, 1, i] = mul(xx00, α^i)
		/// </summary>
		/// <param name="npar"></param>
		/// <returns></returns>
		public ushort[,,] makeDecodeTable(int npar)
		{
			var parityTable = new ushort[256, 2, npar];
			for (int i = 0; i < npar; i++)
			{
				parityTable[0, 0, i] = 0;
				parityTable[0, 1, i] = 0;
			}
			for (int ib = 1; ib < 256; ib++)
			{
				int logib0 = LogTbl[ib];
				int logib1 = LogTbl[ib << 8];
				for (int i = 0; i < npar; i++)
				{
					parityTable[ib, 0, i] = ExpTbl[logib0 + i];
					parityTable[ib, 1, i] = ExpTbl[logib1 + i];
				}
			}
			return parityTable;
		}

		/**
		 * シンドロームの計算
		 * @param data int[]
		 *		入力データ配列
		 * @param length int
		 *		データ長
		 * @param syn int[]
		 *		(x - α^0) (x - α^1) (x - α^2) ...のシンドローム
		 * @return boolean
		 *		true: シンドロームは総て0
		 */
		public bool calcSyndrome(byte[] data, int length, int[] syn)
		{
			int hasErr = 0;
			for (int i = 0; i < syn.Length; i++)
			{
				int wk = 0;
				for (int idx = 0; idx < length; idx++)
				{
					//wk = data[idx] ^ ((wk == 0) ? 0 : expTbl[logTbl[wk] + i + symStart]);		// wk = data + wk * α^i
					wk = data[idx] ^ ((wk == 0) ? 0 : expTbl[logTbl[wk] + i]);		// wk = data + wk * α^i
				}
				syn[i] = wk;
				hasErr |= wk;
			}
			return hasErr == 0;
		}

		/**
		 * シンドロームの計算
		 * @param data int[]
		 *		入力データ配列
		 * @param length int
		 *		データ長
		 * @param syn int[]
		 *		(x - α^0) (x - α^1) (x - α^2) ...のシンドローム
		 * @return boolean
		 *		true: シンドロームは総て0
		 */
		public unsafe bool calcSyndrome(ushort* data, int length, int[] syn)
		{
			int hasErr = 0;
			for (int i = 0; i < syn.Length; i++)
			{
				int wk = 0;
				for (int idx = 0; idx < length; idx++)
				{
					//wk = data[idx] ^ ((wk == 0) ? 0 : expTbl[logTbl[wk] + i + symStart]);		// wk = data + wk * α^i
					wk = data[idx] ^ ((wk == 0) ? 0 : expTbl[logTbl[wk] + i]);		// wk = data + wk * α^i
				}
				syn[i] = wk;
				hasErr |= wk;
			}
			return hasErr == 0;
		}

        public unsafe int doForney(int jisu, int ps, int* sigma, int* omega)
        {
            int zlog = this.Max - this.toLog(ps);					// zのスカラー

            // ω(z)の計算
            int ov = omega[0];
            for (int j = 1; j < jisu; j++)
            {
                ov ^= this.mulExp(omega[j], (zlog * j) % this.Max);		// ov += ωi * z^j
            }

            // σ'(z)の値を計算(σ(z)の形式的微分)
            int dv = sigma[1];
            for (int j = 2; j < jisu; j += 2)
            {
                dv ^= this.mulExp(sigma[j + 1], (zlog * j) % this.Max);	// dv += σ<j+1> * z^j
            }

            /*
             * 誤り訂正 E^i = α^i * ω(z) / σ'(z)
             * 誤り位置の範囲はチェン探索のときに保証されているので、
             * ここではチェックしない
             */
            return this.mul(ps, this.div(ov, dv));
        }
    }

	public class Galois81D: Galois
	{
		public const int POLYNOMIAL = 0x1d;

		public static Galois81D instance = new Galois81D();

		Galois81D()
			: base(POLYNOMIAL, 8)
		{
		}
	}

	public class Galois16 : Galois
	{
		public const int POLYNOMIAL = 0x1100B;

		public static Galois16 instance = new Galois16();

		Galois16()
			: base(POLYNOMIAL, 16)
		{
		}
	}
}
