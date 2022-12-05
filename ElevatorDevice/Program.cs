using ElevatorDevice.Models;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace ElevatorDevice
{
    class Program
    {
        private static DeviceClient _deviceClient;
        private static Twin twin;
        private static string elevatorapi = "https://localhost:7169/api/elevator";
        private static string apiUri = "https://localhost:7169/api";
        public static List<ElevatorItem> elevatorItems;
        private static int _Intervall = 5000;
        private static bool _connected = false;
        private static string _deviceId = "";


        private static async Task Main()
        {
            GetElevators();
            await Setup();
            await Loop();
        }

        private static async void GetElevators()
        {
            using var client = new HttpClient();
            elevatorItems = await client.GetFromJsonAsync<List<ElevatorItem>>(elevatorapi);
        }

        

        private static async Task Setup()
        {
            Console.WriteLine("Initializing Device, Please wait.....");

            await Task.Delay(5000);

            using var client = new HttpClient();
            var response = await client.GetAsync($"{apiUri}/Create");
            var connectionString = response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _deviceClient = DeviceClient.CreateFromConnectionString(connectionString.Result);
                twin = await _deviceClient.GetTwinAsync();
                var twinCollection = new TwinCollection();

                twinCollection["elevators"] = elevatorItems;
                await _deviceClient.UpdateReportedPropertiesAsync(twinCollection);
                await _deviceClient.SetMethodHandlerAsync("ShutDown", ShutDown, _deviceClient);
                await _deviceClient.SetMethodHandlerAsync("DoorAction", DoorAction, _deviceClient);
                await _deviceClient.SetMethodHandlerAsync("Reset", Reset, _deviceClient);
                await _deviceClient.SetMethodHandlerAsync("ChangeFloor", ChangeFloor, _deviceClient);
            }
            _connected = true;
        }
        private static async Task Loop()
        {
            while (true)
            {
                if (_connected)
                {
                    twin = await _deviceClient.GetTwinAsync();
                    Console.WriteLine(twin.ConnectionState.ToString());
                }

                await Task.Delay(_Intervall);
            }

        }
        private static async Task PrintShutDown(string request = null)
        {
            if (request.Equals("null"))
            {
                Console.WriteLine("Request null");
            }
            else
            {
                var result = JsonConvert.DeserializeObject<dynamic>(request);
                Console.WriteLine(result.id);
                List<ElevatorItem> elevators = twin.Properties.Reported["elevators"].ToObject<List<ElevatorItem>>();

                var elevator = elevators.Find(e => e.Id == Convert.ToInt16(result.id));
                Console.WriteLine("was: " + elevator.ShutDown);
                elevator.ShutDown = !elevator.ShutDown;
                if(elevator.ElevatorStatus == "Active")
                {
                    elevator.ElevatorStatus = "Inactive";
                } else
                {
                    elevator.ElevatorStatus = "Active";
                }

                var twinCollection = new TwinCollection();
                twinCollection["elevators"] = elevators;
                await _deviceClient.UpdateReportedPropertiesAsync(twinCollection);

                Console.WriteLine("became: " + elevator.ShutDown);

                using var client = new HttpClient();

                var payload = JsonConvert.SerializeObject(elevator);
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
                await client.PutAsync($"{apiUri}/Elevator/{elevator.Id}", httpContent);
            }
        }
        private static async Task<MethodResponse> ShutDown(MethodRequest request, object userContext)
        {
            try
            {
                await PrintShutDown(request.DataAsJson);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return await Task.FromResult(new MethodResponse(new byte[0], 200));
        }


        private static async Task<MethodResponse> Reset(MethodRequest request, object userContext)
        {
            try
            {
                await PrintReset(request.DataAsJson);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return await Task.FromResult(new MethodResponse(new byte[0], 200));
        }
        private static async Task PrintReset(string request = null)
        {
            if (request.Equals("null"))
            {
                Console.WriteLine("Request null");
            }
            else
            {
                var result = JsonConvert.DeserializeObject<dynamic>(request);
                Console.WriteLine(result.id);
                List<ElevatorItem> elevators = twin.Properties.Reported["elevators"].ToObject<List<ElevatorItem>>();

                var elevator = elevators.Find(e => e.Id == Convert.ToInt16(result.id));
                Console.WriteLine("Reset was : " + elevator.Reboot);
                elevator.Reboot = !elevator.Reboot;
                elevator.Floor = elevator.MinFloor;
                var twinCollection = new TwinCollection();
                twinCollection["elevators"] = elevators;
                await _deviceClient.UpdateReportedPropertiesAsync(twinCollection);

                Console.WriteLine("Reset became : " + elevator.Reboot);

                using var client = new HttpClient();

                var payload = JsonConvert.SerializeObject(elevator);
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
                await client.PutAsync($"{apiUri}/Elevator/{elevator.Id}", httpContent);
            }

        }
        private static async Task PrintOpenDoor(string request = null)
        {
            if (request.Equals("null"))
            {
                Console.WriteLine("Request null");
            }
            else
            {
                var result = JsonConvert.DeserializeObject<dynamic>(request);
                Console.WriteLine(result.id);
                List<ElevatorItem> elevators = twin.Properties.Reported["elevators"].ToObject<List<ElevatorItem>>();

                var elevator = elevators.Find(e => e.Id == Convert.ToInt16(result.id));
                Console.WriteLine("Door was: " + elevator.Door);
                elevator.Door = !elevator.Door;

                var twinCollection = new TwinCollection();
                twinCollection["elevators"] = elevators;
                await _deviceClient.UpdateReportedPropertiesAsync(twinCollection);

                Console.WriteLine("door became: " + elevator.Door);

                using var client = new HttpClient();

                var payload = JsonConvert.SerializeObject(elevator);
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
                await client.PutAsync($"{apiUri}/Elevator/{elevator.Id}", httpContent);
            }

        }

        private static async Task<MethodResponse> DoorAction(MethodRequest request, object userContext)
        {
            try
            {
                await PrintOpenDoor(request.DataAsJson);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return await Task.FromResult(new MethodResponse(new byte[0], 200));
        }
        private static async Task PrintChangeFloor(string request = null)
        {
            if (request.Equals("null"))
            {
                Console.WriteLine("Request null");
            }
            else
            {
                var result = JsonConvert.DeserializeObject<dynamic>(request);
                Console.WriteLine(result.id);
                List<ElevatorItem> elevators = twin.Properties.Reported["elevators"].ToObject<List<ElevatorItem>>();

                var elevator = elevators.Find(e => e.Id == Convert.ToInt16(result.id));
                Console.WriteLine("Floor was: " + elevator.Floor);
                elevator.Floor = result.floor;                                

                var twinCollection = new TwinCollection();
                twinCollection["elevators"] = elevators;
                await _deviceClient.UpdateReportedPropertiesAsync(twinCollection);

                Console.WriteLine("Floor became: " + elevator.Floor);

                using var client = new HttpClient();

                var payload = JsonConvert.SerializeObject(elevator);
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
                await client.PutAsync($"{apiUri}/Elevator/{elevator.Id}", httpContent);
            }

        }

        private static async Task<MethodResponse> ChangeFloor(MethodRequest request, object userContext)
        {
            try
            {
                await PrintChangeFloor(request.DataAsJson);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return await Task.FromResult(new MethodResponse(new byte[0], 200));
        }
    }


}



        

        

