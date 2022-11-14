using ElevatorDevice.Models;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace Device.Tv.Livingroom
{
    class Program
    {
        private static DeviceClient deviceClient;
        private static Twin twin;   
        private static string elevatorapi = "https://agilewebapi.azurewebsites.net/api/Elevator";   
        private static string apiUri = "https://localhost:7169/Create";
        public static List<ElevatorItem> elevatorItems;

        public static async Task Main()
        {
            GetElevators();
            await GetConfigurationAsync();

        }

        private static async void GetElevators()
        {
            Console.Clear();
            Console.WriteLine("Getting All Elevators ... ");

            using var client = new HttpClient();
            elevatorItems = await client.GetFromJsonAsync<List<ElevatorItem>>(elevatorapi);
            
        }

        private static async Task GetConfigurationAsync()
        {
            Console.Clear();
            Console.WriteLine("Getting Connectionstring for Elevator ... ");

            using var client = new HttpClient();
            var response = await client.GetAsync(apiUri);
            var connectionString = response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                deviceClient = DeviceClient.CreateFromConnectionString(connectionString.Result);
                twin = await deviceClient.GetTwinAsync();
                var twinCollection = new TwinCollection();
                
                twinCollection["elevators"] = JsonConvert.SerializeObject(elevatorItems);
                await deviceClient.UpdateReportedPropertiesAsync(twinCollection);
            }
        }

    }
    
}