using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using Newtonsoft.Json;
using services.varian.com.AriaWebConnect.Link;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using VMSType = services.varian.com.AriaWebConnect.Common;
using System.Collections;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

// TODO: Uncomment the following line if the script requires write access.
// [assembly: ESAPIScript(IsWriteable = true)]

namespace WeeklyPhysicsUCHHRHCCMCLPMC
{
    class Beam
    {
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        private int mu;
        public int MU
        {
            get { return mu; }
            set { mu = value; }
        }

        private int? fx;
        public int? Fx
        {
            get { return fx; }
            set { fx = value; }
        }
        private string plan;
        public string Plan
        {
            get { return plan; }
            set { plan = value; }
        }
    }

    class Machine
    {
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        private string dept;
        public string Dept
        {
            get { return dept; }
            set { dept = value; }
        }

        private string hosp;
        public string Hosp
        {
            get { return hosp; }
            set { hosp = value; }
        }

        public Machine(string machineName, string machineDept, string machineHosp)
        {
            name = machineName;
            dept = machineDept;
            hosp = machineHosp;
        }
    }
    class Program
  {
    [STAThread]
    static void Main(string[] args)
    {
      try
      {
        using (Application app = Application.CreateApplication())
        {
          Execute(app, args);
        }
      }
      catch (Exception e)
      {
        Console.Error.WriteLine(e.ToString());
      }
    }
        static void Execute(Application app, string[] args)
        {
            string apiKey = "ea661d43-6a18-41e0-9a7b-8acfa3b2b114";

            Machine UCH_T1 = new Machine("UCH_T1", "Radiation Oncology", "University of Colorado Hospital");
            Machine UCH_T2 = new Machine("UCH_T2", "Radiation Oncology", "University of Colorado Hospital");
            Machine UCH_T3 = new Machine("UCH_T3", "Radiation Oncology", "University of Colorado Hospital");
            Machine STX = new Machine("STX", "Radiation Oncology", "University of Colorado Hospital");
            Machine VS620 = new Machine("VS620", "Radiation Oncology", "University of Colorado Hospital");
            Machine HRH_T1 = new Machine("HRH_T1", "Highlands Ranch Radiation Oncology", "Highlands Ranch Hospital");
            Machine CCMC_TB = new Machine("CCMC_TB", "Cherry Creek Radiation Oncology", "Cherry Creek Medical Center");
            Machine LPMC_T1 = new Machine("LPMC_T1", "Longs Peak Radiation Oncology", "Longs Peak Medical Center");


            DateTime date;
            if (args.Count() != 0) { 
                date = GetPreviousTxDay(apiKey, DateTime.Today.AddDays(0).Date);
            }
            else
            {
                date = DateTime.Today.AddDays(-1).Date;
            }
            Console.WriteLine($"Patients treated on {date}");

            string[] words = { "UCH_CS1", "UCH_CS2", "CCMC_Weekly", "HRH_Weekly", "LPMC_Weekly" };

            CircularLinkedList<string> CS_lists = new CircularLinkedList<string>(words);

            var finalList = CS_lists.GetEnumerator();

            List<Machine> machineList = new List<Machine>() { UCH_T2, STX, UCH_T1, UCH_T3, VS620, HRH_T1, CCMC_TB, LPMC_T1 };

            foreach (Machine currentMachine in machineList)
            {
                Console.WriteLine(currentMachine.Name);
                MachineAppointmentsBasic(apiKey, currentMachine, app, date, ref finalList);
            }


            Console.WriteLine("The end");

        }
        static Boolean IsTxDay(string apiKey, DateTime date)
        {
            List<string> machines = new List<string>();
            machines.Add("UCH_T1");
            machines.Add("UCH_T2");

            foreach (var machine in machines) {
                string departmentName = "Radiation Oncology";
                var hospitalName = "University of Colorado Hospital";
                //string resourceType = "Venue";
                string machineId = machine;
                var startdate = date;
                var enddate = startdate.AddHours(23);
                GetMachineAppointmentsRequest getMachineAppointmentsRequest = new GetMachineAppointmentsRequest
                {
                    DepartmentName = new VMSType.String { Value = departmentName },
                    HospitalName = new VMSType.String { Value = hospitalName },
                    //ResourceType = new VMSType.String { Value = resourceType },
                    MachineId = new VMSType.String { Value = machineId },
                    StartDateTime = new VMSType.String { Value = startdate.ToString("yyyy-MM-ddTHH:mm:sszzz") },
                    EndDateTime = new VMSType.String { Value = enddate.ToString("yyyy-MM-ddTHH:mm:sszzz") },

                };
                string request_appointments = $"{{\"__type\":\"GetMachineAppointmentsRequest:http://services.varian.com/AriaWebConnect/Link\",{JsonConvert.SerializeObject(getMachineAppointmentsRequest).TrimStart('{')}}}";
                string response_appointments = SendData(request_appointments, true, apiKey);
                GetMachineAppointmentsResponse getMachineAppointmentsResponse = JsonConvert.DeserializeObject<GetMachineAppointmentsResponse>(response_appointments);
                if (getMachineAppointmentsResponse.MachineAppointments.Count() == 0)
                    return false;
            }
            return true;

        }
        static DateTime GetPreviousTxDay(string apiKey, DateTime date)
        {
            var newDate = GetPreviousBusinessDay(date);
            if (IsTxDay(apiKey, newDate))
            {
                return newDate;
            }
            else
                return GetPreviousTxDay(apiKey, newDate);
        }

        static DateTime GetPreviousBusinessDay(DateTime date)
        {
            switch((int)date.DayOfWeek)
            {
                case 1:
                    return date.AddDays(-3);
                case 0:
                    return date.AddDays(-2);
                default:
                    return date.AddDays(-1);

            }
        }
        static void MachineAppointmentsBasic(string apiKey, Machine machine, Application app, DateTime date, ref IEnumerator finalList)
        {
            string departmentName = machine.Dept;
            var hospitalName = machine.Hosp;
            string resourceType = "Machine";
            string machineId = machine.Name;
            var startdate = date;
            //var week_start = new DateTimeOffset(startdate, TimeZoneInfo.Local.GetUtcOffset(startdate));
            var enddate = startdate.AddHours(23);
            GetMachineAppointmentsRequest getMachineAppointmentsRequest = new GetMachineAppointmentsRequest
            {
                DepartmentName = new VMSType.String { Value = departmentName },
                HospitalName = new VMSType.String { Value = hospitalName },
                ResourceType = new VMSType.String { Value = resourceType },
                MachineId = new VMSType.String { Value = machineId },
                StartDateTime = new VMSType.String { Value = startdate.ToString("yyyy-MM-ddTHH:mm:sszzz") },
                EndDateTime = new VMSType.String { Value = enddate.ToString("yyyy-MM-ddTHH:mm:sszzz") }
            };
            string request_appointments = $"{{\"__type\":\"GetMachineAppointmentsRequest:http://services.varian.com/AriaWebConnect/Link\",{JsonConvert.SerializeObject(getMachineAppointmentsRequest).TrimStart('{')}}}";
            string response_appointments = SendData(request_appointments, true, apiKey);
            GetMachineAppointmentsResponse getMachineAppointmentsResponse = JsonConvert.DeserializeObject<GetMachineAppointmentsResponse>(response_appointments);
            var nonNullPatientAppointments = getMachineAppointmentsResponse.MachineAppointments.Where(m => (m.PatientId.Value != null) & (m.ActivityStatus.Value.Contains("Complete"))).Select(a => a.PatientId.Value).Distinct();

            foreach (var patientID in nonNullPatientAppointments)
            {
                PatientCourses(apiKey, patientID, startdate, app, ref finalList, machine);
            }
            
        }
        static string PatientCourses(string apiKey, string patientID, DateTime enddate, Application app, ref IEnumerator finalList, Machine machine)
        {

            GetPatientCoursesAndPlanSetupsRequest getPatientCoursesAndPlanSetupsRequest = new GetPatientCoursesAndPlanSetupsRequest
            {
                PatientId = new VMSType.String { Value = patientID },
                TreatmentType = new VMSType.String { Value = "Linac" }
            };
            string request_courses = $"{{\"__type\":\"GetPatientCoursesAndPlanSetupsRequest:http://services.varian.com/AriaWebConnect/Link\",{JsonConvert.SerializeObject(getPatientCoursesAndPlanSetupsRequest).TrimStart('{')}}}";
            string response_courses = SendData(request_courses, true, apiKey);
            GetPatientCoursesAndPlanSetupsResponse getPatientCoursesAndPlanSetupsResponse = JsonConvert.DeserializeObject<GetPatientCoursesAndPlanSetupsResponse>(response_courses);

            var pat = app.OpenPatientById(patientID);
            string message = null;
            Boolean final = false;

            foreach (var course in getPatientCoursesAndPlanSetupsResponse.PatientCourses.Where(c => (c.StartDateTime != null) & (c.CompletedDateTime == null)))
            {
                var startDate = Convert.ToDateTime(course.StartDateTime.Value);
               
                if (startDate.CompareTo(enddate.Date) < 0)
                   
                {
                    List<Beam> beams = new List<Beam>();
                    SortedDictionary<DateTime, int> treatments = new SortedDictionary<DateTime, int>();

                    var plans = pat.Courses.Where(c => c.Id.Equals(course.CourseId.Value)).SelectMany(t => t.PlanSetups);
                    var TxSessions = plans.SelectMany(p => p.TreatmentSessions);
                    var fin = plans.SelectMany(p => p.TreatmentSessions).Where(t => t.HistoryDateTime.Date.CompareTo(enddate.Date)==0);
                    var test = plans.SelectMany(p => p.TreatmentSessions).Where(t => t.HistoryDateTime.Date.CompareTo(enddate.Date) > 0);


                    if (fin.Count() != 0)
                    { 
                        
                    if (TxSessions.Where(t => t.Status == TreatmentSessionStatus.Treat).Count()==0 & test.Count()==0) {
                            final = true;
                        }
                        
                            foreach (var plan in plans.Where(p => p.IsTreated))
                            {
                                {
                                    var sessions = plan.TreatmentSessions.Where(s => (s.Status == TreatmentSessionStatus.Completed) | (s.Status == TreatmentSessionStatus.CompletedPartially)).Select(t => t.HistoryDateTime.Date);
                                    SortedDictionary<DateTime, int> tx = new SortedDictionary<DateTime, int>();

                                    foreach (var session in sessions)
                                    {
                                        if (tx.Keys.Contains(session))
                                        {
                                            tx[session]++;
                                        }
                                        else
                                        {
                                            tx.Add(session, 1);
                                        }
                                    }
                                    foreach (var T in tx.Keys)
                                    {
                                        if (treatments.Keys.Contains(T))
                                        {
                                            treatments[T] = Math.Max(treatments[T], tx[T]);
                                        }
                                        else
                                        {
                                            treatments.Add(T, tx[T]);
                                        }
                                    }
                                }
                            
                        }
                    }
                    int sum = 0;
                    SortedDictionary<DateTime, int> treatmentsCum = new SortedDictionary<DateTime, int>();
                    
                    foreach (var t in treatments)
                    {
                        sum = sum + t.Value;
                        treatmentsCum.Add(t.Key, sum);
                    }
                    if (treatmentsCum.ContainsKey(enddate.Date))
                    {
                        int numFx = treatmentsCum[enddate.Date];
                        
                        if (final)
                        {
                            if ((numFx % 5 > 2)|(numFx % 5 == 0))
                            {
                                Console.WriteLine($"{patientID} Final billable Fx# {numFx% 5}");
                                CSlist(apiKey, patientID, enddate.Date, finalList.Current.ToString(), "Physics Final Check Billable", machine);
                               
                            }
                            else
                            {
                                Console.WriteLine($"{patientID} Final non billable Fx# {numFx % 5}");
                                CSlist(apiKey, patientID, enddate.Date, finalList.Current.ToString(), "Physics Final Check non Billable", machine);

                            }
                            finalList.MoveNext();
                        }
                        else
                        {
                            for(int ii = 0; ii < treatments[enddate.Date]; ii++) {
                                if ((numFx - ii) % 5 == 0)
                                {
                                    Console.WriteLine($"{patientID} Weekly Physics Check");
                                    CSlist(apiKey, patientID, enddate.Date, finalList.Current.ToString(), "Weekly Physics Check", machine);
                                    finalList.MoveNext();
                                }
                            }
                        }
                    }
                }
            }
            app.ClosePatient();
            return message;

        }
        
        public static void CSlist(string apiKey, string PatientID, DateTime date, string list, string activity, Machine machine)
        {
           
            var startdate = date.AddHours(1);
            var enddate = startdate.AddMinutes(15);


                CreateMachineAppointmentRequest createMachineAppointmentRequest = new CreateMachineAppointmentRequest
                {
                    ActivityName = new VMSType.String { Value = activity },
                    DepartmentName = new VMSType.String { Value = machine.Dept },
                    HospitalName = new VMSType.String { Value = machine.Hosp },
                    ResourceType = new VMSType.String { Value = "Venue" },
                    MachineId = new VMSType.String { Value = list },
                    PatientId = new VMSType.String { Value = PatientID },
                    StartDateTime = new VMSType.String { Value = startdate.ToString("yyyy-MM-ddTHH:mm:sszzz") },
                    EndDateTime = new VMSType.String { Value = enddate.ToString("yyyy-MM-ddTHH:mm:sszzz") },
                    ActivityNote = new VMSType.String { Value = "Weekly physics chart check checking accuracy of treatment delivery and accuracy of treatment setup" }
                };


            Console.WriteLine(list);

                string request_appointment = $"{{\"__type\":\"CreateMachineAppointmentRequest:http://services.varian.com/AriaWebConnect/Link\",{JsonConvert.SerializeObject(createMachineAppointmentRequest).TrimStart('{')}}}";
            string response_appointment = SendData(request_appointment, true, apiKey);
        }

        public static string SendData(string request, bool bIsJson, string apiKey)
        {
            var sMediaTYpe = bIsJson ? "application/json" :
           "application/xml";
            var sResponse = System.String.Empty;
            using (var c = new HttpClient(new
           HttpClientHandler()
            { UseDefaultCredentials = true }))
            {
                if (c.DefaultRequestHeaders.Contains("ApiKey"))
                {
                    c.DefaultRequestHeaders.Remove("ApiKey");
                }
                c.DefaultRequestHeaders.Add("ApiKey", apiKey);
                //in App.Config, change this to the Resource ID for your REST Service.
                var task =
               c.PostAsync(ConfigurationManager.AppSettings["GatewayRestUrl"],
                new StringContent(request, Encoding.UTF8,
               sMediaTYpe));
                Task.WaitAll(task);
                var responseTask =
               task.Result.Content.ReadAsStringAsync();
                Task.WaitAll(responseTask);
                sResponse = responseTask.Result;
            }
            return sResponse;
        }

    }
    public class CircularLinkedList<T> : LinkedList<T>
    {
        public new IEnumerator GetEnumerator()
        {
            return new CircularLinkedListEnumerator<T>(this);
        }

        public CircularLinkedList(IEnumerable<T> p) : base(p)
        {

        }
    }

    public class CircularLinkedListEnumerator<T> : IEnumerator<T>
    {
        private LinkedListNode<T> _current;
        public T Current => _current.Value;
        object IEnumerator.Current => Current;

        public CircularLinkedListEnumerator(LinkedList<T> list)
        {
            _current = list.First;
        }

        public bool MoveNext()
        {
            if (_current == null)
            {
                return false;
            }

            _current = _current.Next ?? _current.List.First;
            return true;
        }

        public void Reset()
        {
            _current = _current.List.First;
        }

        public void Dispose() { }
    }

    public static class CircularLinkedListExtensions
    {
        public static LinkedListNode<T> Next<T>(this LinkedListNode<T> node)
        {
            if (node != null && node.List != null)
            {
                return node.Next ?? node.List.First;
            }

            return null;
        }

        public static LinkedListNode<T> Previous<T>(this LinkedListNode<T> node)
        {
            if (node != null && node.List != null)
            {
                return node.Previous ?? node.List.Last;
            }
            return null;
        }
    }
}
