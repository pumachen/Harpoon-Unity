using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Harpoon.USD
{
    [CreateAssetMenu(fileName = "USDMaterialBaker", menuName = "Harpoon/USD Material Baker")]
    public class USDMaterialBaker : ScriptableObject
    {
        [System.Serializable]
        public struct BakerRule
        {
            public Shader shader;
            public Shader baker;
        }

        public BakerRule[] rules;

        public Shader this[Shader shader]
        {
            get
            {
                foreach (var rule in rules)
                {
                    if (rule.shader == shader)
                        return rule.baker;
                }
                return null;
            }
        }
    }
}