using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace SkyOfFreedom.Data
{
    [CreateAssetMenu(fileName = "NewComponent", menuName = "Sky of Freedom/Data/Component")]

    public class ComponentSO : DataSO
    {
        [Header("General")]

        [SerializeField]
        private string componentName;

        [SerializeField]
        private Sprite icon;

        [Header("Requirements")]

        [SerializeField]
        [TextArea]
        private string description;

        [Header("Production")]

        [SerializeField]
        [Min(0.1f)]
        private float productionTime = 1f;

        [SerializeField]
        private List<MaterialAmount> recipe = new();

        public Sprite Icon
        {
            get
            {
                return icon;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public float ProductionTime
        {
            get
            {
                return productionTime;
            }
        }

        public IReadOnlyList<MaterialAmount> Recipe
        {
            get
            {
                return recipe;
            }
        }

        public int ProductionCost
        {
            get
            {
                int total = 0;

                foreach (MaterialAmount material in recipe)
                {
                    if (material.Material == null)
                    {
                        continue;
                    }

                    total += material.Material.BasePrice * material.Amount;
                }

                return total;
            }
        }

#if UNITY_EDITOR
        public void SetData(
            string id,
            string componentName,
            string description,
            float productionTime,
            List<MaterialAmount> recipe)
        {
            SetID(id);

            this.componentName = componentName;
            this.description = description;
            this.productionTime = productionTime;

            this.recipe.Clear();
            this.recipe.AddRange(recipe);
        }
#endif

    }
}