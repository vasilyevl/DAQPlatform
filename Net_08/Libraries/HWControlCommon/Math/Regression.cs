/*
Copyright (c) 2024 vasilyevl (Grumpy). Permission is hereby granted, 
free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"),to deal in the Software 
without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the 
Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

LinearRegression method was originally created by Nikolay Kostov 
See the original code and licence here:
    https://gist.github.com/NikolayIT/d86118a3a0cb3f5ed63d674a350d75f2
*/


namespace Utilities.Math
{
    public static class Regression
    {
        /// <summary>
        /// Fits a line to a collection of (x,y) points.
        /// This implementation was originally created by Nikolay Kostov
        /// See the original code and licence here:
        /// https://gist.github.com/NikolayIT/d86118a3a0cb3f5ed63d674a350d75f2
        /// </summary>
        /// <param name="xVals">The x-axis values.</param>
        /// <param name="yVals">The y-axis values.</param>
        /// <param name="rSquared">The r^2 value of the line.</param>
        /// <param name="yIntercept">The y-intercept value of the line 
        /// (i.e. y = ax + b, yIntercept is b).</param>
        /// <param name="slope">The slop of the line 
        /// (i.e. y = ax + b, slope is a).</param>
        public static void LinearLSF(double[] xVals, double[] yVals,
                out double rSquared, out double yIntercept, out double slope) {

            if(xVals == null ||yVals == null) {

                throw new ArgumentNullException("Input can't be null.");
            }

            if (xVals.Length != yVals.Length) {

                throw new ArgumentException($"Input X and Y arrays " +
                    $"must have the same length.");
            }

            if (xVals.Length < 2) {

                throw new ArgumentException("Input must " +
                    "have at least 2 points."); 
            }

            rSquared = double.NaN;
            yIntercept = double.NaN;
            slope = double.NaN;

            double sumOfX = 0;
            double sumOfY = 0;

            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double sumCodeviates = 0;
            int numberOfSamples = xVals.Length;


            for ( var i = 0; i < numberOfSamples; i++) {

                sumCodeviates += xVals[i] * yVals[i];
                sumOfX += xVals[i];
                sumOfY += yVals[i];
                sumOfXSq += xVals[i] * xVals[i];
                sumOfYSq += yVals[i] * yVals[i];
            }            
           // double ssY = sumOfYSq - ((sumOfY * sumOfY) / count);

            double rNumerator = (numberOfSamples * sumCodeviates) - 
                                                    (sumOfX * sumOfY);
            double rDenom = (numberOfSamples * sumOfXSq - (sumOfX * sumOfX)) * 
                            (numberOfSamples * sumOfYSq - (sumOfY * sumOfY));
            double sCo = sumCodeviates - ((sumOfX * sumOfY) / numberOfSamples);

            double ssX = sumOfXSq - ((sumOfX * sumOfX) / numberOfSamples);

            rSquared =  rNumerator * rNumerator/ rDenom;
            yIntercept = (sumOfY / numberOfSamples) - 
                            ((sCo / ssX) * (sumOfY / numberOfSamples));
            slope = sCo / ssX;
        }
        
    }

}
