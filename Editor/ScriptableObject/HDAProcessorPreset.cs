using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Harpoon
{
    public class HDAProcessorPreset : ScriptableObject
    {
        public string hda;
        public IntParm[] intParms;
        public FloatParm[] floatParms;
        public StringParm[] stringParms;

        public IEnumerable<HouParm> parms
        {
            get
            {
                foreach (var intParm in intParms)
                    yield return intParm;
                foreach (var floatParm in floatParms)
                    yield return floatParm;
                foreach (var stringParm in stringParms)
                    yield return stringParm;
            }
        }
    }
}