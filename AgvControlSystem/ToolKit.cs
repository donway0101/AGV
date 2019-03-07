
using System;

namespace AgvControlSystem
{
    public class ToolKit
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bitPosition">0 based index</param>
        public static bool GetBit(int value, int bitPosition)
        {
            return (value & (1 << bitPosition)) != 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bitPosition">0 based index</param>
        public static bool GetBit(double value, int bitPosition)
        {
            int tempInt;
            try
            {
                tempInt = Convert.ToInt32(value);
            }
            catch (Exception)
            {

                throw;
            }
            return (tempInt & (1 << bitPosition)) != 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bitPosition">0 based index</param>
        public static void SetBit(ref int value, int bitPosition)
        {
            value |= 1 << bitPosition;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bitPosition">0 based index</param>
        public static void SetBit(ref double value, int bitPosition)
        {
            int tempInt;
            try
            {
                tempInt = Convert.ToInt32(value);
            }
            catch (Exception)
            {

                throw;
            }
            tempInt |= 1 << bitPosition;
            value = tempInt;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bitPosition">0 based index</param>
        public static void ResetBit(ref int value, int bitPosition)
        {
            value &= ~(1 << bitPosition);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bitPosition">0 based index</param>
        public static void ResetBit(ref double value, int bitPosition)
        {
            int tempInt;
            try
            {
                tempInt = Convert.ToInt32(value);
            }
            catch (Exception)
            {

                throw;
            }
            tempInt &= ~(1 << bitPosition);
            value = tempInt;
        }

        public static int MaskOutInput(int value)
        {
            double mask = Math.Pow(2.0, 16.0)-1;
            value &= (int)mask;
            return value;
        }
    }
}
