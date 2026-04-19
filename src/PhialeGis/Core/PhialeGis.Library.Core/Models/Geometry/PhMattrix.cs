using System;

namespace PhialeGis.Library.Core.Models.Geometry
{
    /// <summary>
    /// Represents a 3x3 matrix with basic arithmetic operations.
    /// </summary>
    public struct PhMatrix
    {
        private double[,] matrix;

        internal double ScaleX
        {
            get { return matrix[0, 0]; }
        }

        internal double ScaleY
        {
            get { return matrix[1, 1]; }
        }

        internal double TranslateX
        {
            get { return matrix[0, 2]; }
        }

        internal double TranslateY
        {
            get { return matrix[1, 2]; }
        }

        /// <summary>
        /// Initializes a new instance of the PhMatrix struct with the specified initialValues.
        /// </summary>
        /// <param name="initialValues">The initial values for the 3x3 matrix.</param>
        public PhMatrix(double[,] initialValues)
        {
            if (initialValues.GetLength(0) != 3 || initialValues.GetLength(1) != 3)
            {
                throw new ArgumentException("Matrix must be 3x3 in size.");
            }

            matrix = new double[3, 3];
            Array.Copy(initialValues, matrix, initialValues.Length);
        }

        public double this[int row, int column]
        {
            get { return matrix[row, column]; }
            set { matrix[row, column] = value; }
        }

        /// <summary>
        /// Adds two matrices.
        /// </summary>
        /// <param name="a">The first matrix.</param>
        /// <param name="b">The second matrix.</param>
        /// <returns>The sum of the two matrices.</returns>
        public static PhMatrix operator +(PhMatrix a, PhMatrix b)
        {
            var result = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    result[i, j] = a[i, j] + b[i, j];
                }
            }
            return new PhMatrix(result);
        }

        /// <summary>
        /// Multiplies two matrices.
        /// </summary>
        /// <param name="a">The first matrix.</param>
        /// <param name="b">The second matrix.</param>
        /// <returns>The product of the two matrices.</returns>
        public static PhMatrix operator *(PhMatrix a, PhMatrix b)
        {
            var result = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    result[i, j] = a[i, 0] * b[0, j] + a[i, 1] * b[1, j] + a[i, 2] * b[2, j];
                }
            }
            return new PhMatrix(result);
        }

        /// <summary>
        /// Calculates the inverse of the matrix.
        /// </summary>
        /// <returns>The inverse of this matrix.</returns>
        internal PhMatrix Inverse()
        {
            double det = Determinant();
            if (Math.Abs(det) < 1e-9) // Handle near-zero determinant
            {
                throw new InvalidOperationException("Matrix is not invertible.");
            }

            var inverse = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    inverse[i, j] = Cofactor(j, i) / det;
                }
            }

            return new PhMatrix(inverse);
        }

        /// <summary>
        /// Calculates the determinant of the matrix.
        /// </summary>
        /// <returns>The determinant of the matrix.</returns>
        private double Determinant()
        {
            // Calculate determinant for 3x3 matrix
            return matrix[0, 0] * (matrix[1, 1] * matrix[2, 2] - matrix[1, 2] * matrix[2, 1]) -
                   matrix[0, 1] * (matrix[1, 0] * matrix[2, 2] - matrix[1, 2] * matrix[2, 0]) +
                   matrix[0, 2] * (matrix[1, 0] * matrix[2, 1] - matrix[1, 1] * matrix[2, 0]);
        }

        /// <summary>
        /// Calculates the cofactor of an element.
        /// </summary>
        /// <param name="row">The row of the element.</param>
        /// <param name="col">The column of the element.</param>
        /// <returns>The cofactor of the element at the specified row and column.</returns>
        private double Cofactor(int row, int col)
        {
            return Math.Pow(-1, row + col) * Minor(row, col);
        }

        /// <summary>
        /// Calculates the minor of an element.
        /// </summary>
        /// <param name="row">The row of the element.</param>
        /// <param name="col">The column of the element.</param>
        /// <returns>The minor of the element at the specified row and column.</returns>
        private double Minor(int row, int col)
        {
            int x1 = (row == 0) ? 1 : 0;
            int x2 = (row == 2) ? 1 : 2;
            int y1 = (col == 0) ? 1 : 0;
            int y2 = (col == 2) ? 1 : 2;

            return matrix[x1, y1] * matrix[x2, y2] - matrix[x1, y2] * matrix[x2, y1];
        }
    }
}