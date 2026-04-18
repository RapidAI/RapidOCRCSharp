using System;
using System.Collections.Generic;
using System.Text;

namespace RapidOCRLib.Models
{
    public class ModeOptions
    {
        /// <summary>
        /// The path of the Det model file.
        /// </summary>
        public string DetPath { get; set; }
        /// <summary>
        /// The path of the Cls model file.
        /// </summary>
        public string ClsPath { get; set; }
        /// <summary>
        /// The path of the Rec model file.
        /// </summary>
        public string RecPath { get; set; }
        /// <summary>
        /// The path of the Key dictionary file.
        /// </summary>
        public string KeyDicPath { get; set; }
        /// <summary>
        /// cpu thread number,default is 70% of toal logic cpu core numbers.
        /// </summary>
        public int ThreadNum { get; set; } = (int)(Environment.ProcessorCount * 0.7);
    }
}
