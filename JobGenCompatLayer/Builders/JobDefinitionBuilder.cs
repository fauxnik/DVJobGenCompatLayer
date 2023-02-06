using DV.Logic.Job;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JobGenCompatLayer.Builders
{
    /**
     * TODO: document public API class
     */
    public abstract class JobDefinitionBuilder
    {
        public abstract bool IsValid { get; }
        public abstract StaticJobDefinition Build(GameObject gameObject, List<Car> spawnedCars);

        public static class Utils
        {
            public static PaymentCalculationData ExtractPaymentCalculationData(List<TrainCarType> carTypes, List<CargoType> cargoTypes = null)
            {
                if (carTypes == null) { return null; }

                if (cargoTypes == null) { cargoTypes = new List<CargoType>(); }

                var countPerCarType = carTypes.Aggregate(new Dictionary<TrainCarType, int>(), (dictionary, carType) =>
                {
                    if (!dictionary.ContainsKey(carType)) { dictionary[carType] = 0; }
                    dictionary[carType]++;
                    return dictionary;
                });

                var countPerCargoType = cargoTypes.Aggregate(new Dictionary<CargoType, int>(), (dictionary, cargoType) =>
                {
                    if (!dictionary.ContainsKey(cargoType)) { dictionary[cargoType] = 0; }
                    dictionary[cargoType]++;
                    return dictionary;
                });

                return new PaymentCalculationData(countPerCarType, countPerCargoType);
            }

            public static JobLicenses GetRequiredLicensesForCarTypes(List<TrainCarType> carTypes)
            {
                var hashedContainerTypes = carTypes.Aggregate(new HashSet<CargoContainerType>(), (hashSet, carType) =>
                {
                    hashSet.Add(CargoTypes.CarTypeToContainerType[carType]);
                    return hashSet;
                });

                return LicenseManager.GetRequiredLicensesForCarContainerTypes(hashedContainerTypes);
            }

            public static List<CarsPerCargoType> BuildWarehouseData(List<Car> spawnedCars, List<CargoType> cargoTypes, List<float> cargoAmounts)
            {
                if (spawnedCars.Count != cargoTypes.Count || cargoTypes.Count != cargoAmounts.Count)
                {
                    Debug.LogError(() => $"Number of spawned cars ({spawnedCars.Count}), cargo types ({cargoTypes.Count}), and cargo amounts ({cargoAmounts.Count}) disagree.");
                    return null;
                }

                var stagingDictionary = new Dictionary<CargoType, (List<Car>, float)>();

                for (int i = 0; i < cargoTypes.Count; ++i)
                {
                    Car car = spawnedCars[i];
                    CargoType cargoType = cargoTypes[i];
                    float cargoAmount = cargoAmounts[i];

                    if (!stagingDictionary.ContainsKey(cargoType)) { stagingDictionary.Add(cargoType, (new List<Car>(), 0f)); }

                    var carsAndTotalCargo = stagingDictionary[cargoType];
                    var cars = carsAndTotalCargo.Item1;
                    var totalCargo = carsAndTotalCargo.Item2;
                    cars.Add(car);
                    stagingDictionary[cargoType] = (cars, totalCargo + cargoAmount);
                }

                var warehouseData = new List<CarsPerCargoType>();

                foreach (var pair in stagingDictionary)
                {
                    var cargoType = pair.Key;
                    var cars = pair.Value.Item1;
                    var totalCargo = pair.Value.Item2;

                    warehouseData.Add(new CarsPerCargoType(cargoType, cars, totalCargo));
                }

                return warehouseData;
            }

            public static List<CarsPerTrack> GroupCarsByTrack(List<Car> spawnedCars, Dictionary<Track, List<int>> indicesPerTrack)
            {
                var carsPerEndTrack = new List<CarsPerTrack>();

                foreach (var pair in indicesPerTrack)
                {
                    var track = pair.Key;
                    var cars = new List<Car>();

                    foreach (var index in pair.Value)
                    {
                        if (index >= spawnedCars.Count)
                        {
                            Debug.LogError(() => $"Index ({index}) out of range ({spawnedCars.Count}) of spawnedCars.");
                            return null;
                        }

                        var car = spawnedCars[index];
                        if (car == null)
                        {
                            Debug.LogError(() => $"Missing spawned car at index ({index}).");
                        }

                        cars.Add(car);
                    }

                    var carsPerCargoType = new CarsPerTrack(track, cars);
                }

                return carsPerEndTrack;
            }
        }
    }
}
