using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Data
{
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "Sky of Freedom/Game Database")]
    public class GameDatabase : ScriptableObject
    {
        [Header("Data")]

        [SerializeField]
        private List<MaterialSO> materials = new();

        [SerializeField]
        private List<ComponentSO> components = new();

        [SerializeField]
        private List<DroneModelSO> droneModels = new();

        [SerializeField]
        private List<ResearchSO> researches = new();

        private Dictionary<string, MaterialSO> materialLookup;
        private Dictionary<string, ComponentSO> componentLookup;
        private Dictionary<string, DroneModelSO> droneModelLookup;
        private Dictionary<string, ResearchSO> researchLookup;

        private bool isInitialized;

        public IReadOnlyList<MaterialSO> Materials
        {
            get
            {
                return materials;
            }
        }

        public IReadOnlyList<ComponentSO> Components
        {
            get
            {
                return components;
            }
        }

        public IReadOnlyList<DroneModelSO> DroneModels
        {
            get
            {
                return droneModels;
            }
        }

        public IReadOnlyList<ResearchSO> Researches
        {
            get
            {
                return researches;
            }
        }

        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            materialLookup = new Dictionary<string, MaterialSO>();

            foreach (MaterialSO material in materials)
            {
                if (material == null)
                {
                    continue;
                }

                if (materialLookup.ContainsKey(material.ID))
                {
                    Debug.LogError($"Duplicate Material ID: {material.ID}", this);
                    continue;
                }

                materialLookup.Add(material.ID, material);
            }

            componentLookup = new Dictionary<string, ComponentSO>();

            foreach (ComponentSO component in components)
            {
                if (component == null)
                {
                    continue;
                }

                if (componentLookup.ContainsKey(component.ID))
                {
                    Debug.LogError($"Duplicate Component ID: {component.ID}", this);
                    continue;
                }

                componentLookup.Add(component.ID, component);
            }

            droneModelLookup = new Dictionary<string, DroneModelSO>();

            foreach (DroneModelSO droneModel in droneModels)
            {
                if (droneModel == null)
                {
                    continue;
                }

                if (droneModelLookup.ContainsKey(droneModel.ID))
                {
                    Debug.LogError($"Duplicate Drone Model ID: {droneModel.ID}", this);
                    continue;
                }

                droneModelLookup.Add(droneModel.ID, droneModel);
            }

            researchLookup = new Dictionary<string, ResearchSO>();

            foreach (ResearchSO research in researches)
            {
                if (research == null)
                {
                    continue;
                }

                if (researchLookup.ContainsKey(research.ID))
                {
                    Debug.LogError($"Duplicate Research ID: {research.ID}", this);
                    continue;
                }

                researchLookup.Add(research.ID, research);
            }

            isInitialized = true;
        }

        public MaterialSO GetMaterial(string id)
        {
            Initialize();

            materialLookup.TryGetValue(id, out MaterialSO material);

            return material;
        }

        public ComponentSO GetComponent(string id)
        {
            Initialize();

            componentLookup.TryGetValue(id, out ComponentSO component);

            return component;
        }

        public DroneModelSO GetDroneModel(string id)
        {
            Initialize();

            droneModelLookup.TryGetValue(id, out DroneModelSO droneModel);

            return droneModel;
        }

        public ResearchSO GetResearch(string id)
        {
            Initialize();

            researchLookup.TryGetValue(id, out ResearchSO research);

            return research;
        }
    }
}