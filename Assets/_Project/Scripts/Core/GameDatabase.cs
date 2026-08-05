using SkyOfFreedom.Contracts;
using System;
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

        [SerializeField]
        private List<LicenseSO> licenses = new();
        [SerializeField]
        private List<ContractSO> contracts = new();
        [SerializeField]
        private WarehouseConfigSO warehouseConfig;

        private Dictionary<string, MaterialSO> materialLookup;
        private Dictionary<string, ComponentSO> componentLookup;
        private Dictionary<string, DroneModelSO> droneModelLookup;
        private Dictionary<string, ResearchSO> researchLookup;
        private Dictionary<string, LicenseSO> licenseLookup;

        private Dictionary<string, DataSO> dataLookup;

        public IReadOnlyList<LicenseSO> Licenses
        {
            get
            {
                return licenses;
            }
        }

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
        public IReadOnlyList<ContractSO> Contracts
        {
            get
            {
                return contracts;
            }
        }
        public WarehouseConfigSO WarehouseConfig
        {
            get
            {
                return warehouseConfig;
            }
        }
        public void Initialize()
        {
            materialLookup = new Dictionary<string, MaterialSO>();
            componentLookup = new Dictionary<string, ComponentSO>();
            droneModelLookup = new Dictionary<string, DroneModelSO>();
            researchLookup = new Dictionary<string, ResearchSO>();
            licenseLookup = new Dictionary<string, LicenseSO>();
            dataLookup = new Dictionary<string, DataSO>();

            foreach (MaterialSO material in materials)
            {
                if (material == null)
                    continue;

                if (materialLookup.ContainsKey(material.ID))
                {
                    Debug.LogError($"Duplicate Material ID: {material.ID}", this);
                    continue;
                }

                materialLookup.Add(material.ID, material);
                dataLookup.Add(material.ID, material);
            }

            foreach (ComponentSO component in components)
            {
                if (component == null)
                    continue;

                if (componentLookup.ContainsKey(component.ID))
                {
                    Debug.LogError($"Duplicate Component ID: {component.ID}", this);
                    continue;
                }

                componentLookup.Add(component.ID, component);
                dataLookup.Add(component.ID, component);
            }

            foreach (DroneModelSO droneModel in droneModels)
            {
                if (droneModel == null)
                    continue;

                if (droneModelLookup.ContainsKey(droneModel.ID))
                {
                    Debug.LogError($"Duplicate Drone Model ID: {droneModel.ID}", this);
                    continue;
                }

                droneModelLookup.Add(droneModel.ID, droneModel);
                dataLookup.Add(droneModel.ID, droneModel);
            }

            foreach (ResearchSO research in researches)
            {
                if (research == null)
                    continue;

                if (researchLookup.ContainsKey(research.ID))
                {
                    Debug.LogError($"Duplicate Research ID: {research.ID}", this);
                    continue;
                }

                researchLookup.Add(research.ID, research);
            }

            foreach (LicenseSO license in licenses)
            {
                if (license == null)
                    continue;

                if (licenseLookup.ContainsKey(license.ID))
                {
                    Debug.LogError($"Duplicate License ID: {license.ID}", this);
                    continue;
                }

                licenseLookup.Add(license.ID, license);
            }
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
        public LicenseSO GetLicense(string id)
        {   
            Initialize();

            licenseLookup.TryGetValue(id, out LicenseSO license);

            return license;
        }
        public DataSO GetData(string id)
        {

            Initialize();


            dataLookup.TryGetValue(id, out DataSO data);

            return data;
        }

        internal DataSO GetData(object iD)
        {
            throw new NotImplementedException();
        }
    }
}