using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Queues;
using Structures;

namespace CourseWork
{
    class Model : INotifyPropertyChanged
    {
        public readonly Statistics statistics;
        public readonly SystemClock clock;
        public readonly Resource cpu;
        public readonly Resource firstDevice;
        public readonly Resource secondDevice;
        public readonly Memory ram;
        private IdGenerator idGen;
        public SortedLinkedList<Process> sortLinkedList;
        public SortedLinkedList<Process> SortLinkedList
        {
            get { return sortLinkedList; }
            set { sortLinkedList = value; OnPropertyChanged(); }
        }
        public SortedLinkedList<Process> firstDeviceList;
        public SortedLinkedList<Process> FirstDeviceList
        {
            get { return firstDeviceList; }
            set { firstDeviceList = value; OnPropertyChanged(); }
        }
        public SortedLinkedList<Process> secondDeviceList;
        public SortedLinkedList<Process> SecondDeviceList
        {
            get { return secondDeviceList; }
            set { secondDeviceList = value; OnPropertyChanged(); }
        }
        private CPUScheduler cpuScheduler;
        private MemoryManager memoryManager;
        private DeviceScheduler firstDeviceScheduler;
        private DeviceScheduler secondDeviceScheduler;
        private Random processRand;
        public Settings modelSettings;
        private Random rand = new Random();

        public Model()
        {
            clock = new SystemClock();
            statistics = new Statistics(clock);
            cpu = new Resource();
            firstDevice = new Resource();
            secondDevice = new Resource();
            ram = new Memory();
            idGen = new IdGenerator();
            sortLinkedList = new SortedLinkedList<Process>();
            firstDeviceList = new SortedLinkedList<Process>();
            secondDeviceList = new SortedLinkedList<Process>();
            cpuScheduler = new CPUScheduler(cpu, sortLinkedList);
            memoryManager = new MemoryManager();
            firstDeviceScheduler = new DeviceScheduler(firstDevice, firstDeviceList);
            secondDeviceScheduler = new DeviceScheduler(secondDevice, secondDeviceList);
            processRand = new Random();
            modelSettings = new Settings();
        }

        public void SaveSettings()
        {
            ram.Save(modelSettings.ValueOfRAMSize);
            memoryManager.Save(ram);
        }

        public void WorkingCycle()
        {
            clock.WorkingCycle();
            if (processRand.NextDouble() <= modelSettings.Intensity)
            {
                Process proc = new Process(idGen.Id,
                    processRand.Next(modelSettings.MinValueOfAddrSpace, modelSettings.MaxValueOfAddrSpace + 1),
                    processRand.Next(1, modelSettings.LowPriority + 1));
                if (memoryManager.Allocate(proc) != null)
                {
                    proc.BurstTime = processRand.Next(modelSettings.MinValueOfBurstTime,
                        modelSettings.MaxValueOfBurstTime + 1);
                    Subscribe(proc);
                    SortLinkedList = (SortedLinkedList<Process>)sortLinkedList.Put(proc);
                    if (cpu.IsFree())
                    {
                        sortLinkedList = cpuScheduler.Session();
                    }
                }
            }
            cpu.WorkingCycle();
            firstDevice.WorkingCycle();
            secondDevice.WorkingCycle();
            if (cpu.IsFree())
            {
                statistics.IncCPUFreeTime();
            }
            statistics.IncArrivalProcCount();
        }

        public void Clear()
        {
            statistics.Clear();
            clock.Clear();
            cpu.Clear();
            firstDevice.Clear();
            secondDevice.Clear();
            ram.Clear();
            SortLinkedList = (SortedLinkedList<Process>)sortLinkedList.Clear();
            FirstDeviceList = (SortedLinkedList<Process>)firstDeviceList.Clear();
            SecondDeviceList = (SortedLinkedList<Process>)secondDeviceList.Clear();
        }

        private void FreeingAResourceEventHandler(object sender, EventArgs e)
        {
            Process resourceFreeingProcess = sender as Process;

            switch (resourceFreeingProcess.Status) 
            {
                case ProcessStatus.terminated:
                    Unsubscribe(resourceFreeingProcess);
                    cpu.Clear();
                    memoryManager.Free(resourceFreeingProcess);
                    if (sortLinkedList.Count != 0)
                    {
                        putProcessOnResource(cpu);
                    }
                    statistics.IncTerminatedProcCount();
                    break;
                case ProcessStatus.waiting:
                    cpu.Clear();
                    if (sortLinkedList.Count != 0)
                    {
                        putProcessOnResource(cpu);
                    }
                    resourceFreeingProcess.ResetWorkTime();
                    resourceFreeingProcess.BurstTime = processRand.Next(modelSettings.MinValueOfBurstTime, modelSettings.MaxValueOfBurstTime + 1);
                    PutOnDeviceQueue(resourceFreeingProcess);
                    if (firstDevice.IsFree())
                    {
                        putProcessOnResource(firstDevice);
                    }
                    if (secondDevice.IsFree())
                    {
                        putProcessOnResource(secondDevice);
                    }                    
                    break;
                case ProcessStatus.ready:
                    if (resourceFreeingProcess == firstDevice.ActiveProcess)
                    {
                        firstDevice.Clear();
                        putProcessOnResource(firstDevice);
                    }
                    else
                    {
                        secondDevice.Clear();
                        putProcessOnResource(secondDevice);
                    }
                    resourceFreeingProcess.ResetWorkTime();
                    resourceFreeingProcess.BurstTime = processRand.Next(modelSettings.MinValueOfBurstTime, modelSettings.MaxValueOfBurstTime + 1);
                    SortLinkedList = (SortedLinkedList<Process>)sortLinkedList.Put(resourceFreeingProcess);
                    if (cpu.IsFree())
                    {
                        putProcessOnResource(cpu);
                    }
                    break;
                default:
                    throw new Exception("Unknown process status.");
            }
        }

        private void PutOnDeviceQueue(Process resourceFreeingProcess)
        {
            if (Cost(firstDeviceList) <= Cost(secondDeviceList))
                FirstDeviceList = (SortedLinkedList<Process>)FirstDeviceList.Put(resourceFreeingProcess);
            else
                SecondDeviceList = (SortedLinkedList<Process>)SecondDeviceList.Put(resourceFreeingProcess);
        }

        private int Cost(SortedLinkedList<Process> deviceList)
        {
            int[] priority = new int[modelSettings.LowPriority];
            foreach (var process in deviceList.ToArray())
            {
                priority[process.Priority - 1]++;
            }
            int result = 0;
            for (int i = 0; i < priority.Length; i++)
            {
                result += priority[i] * (int)Math.Pow(10, i);
            }
            return result;
        }

        private void putProcessOnResource(Resource resource)
        {
            if (resource == cpu)
            {
                SortLinkedList = cpuScheduler.Session();
            }
            else if (resource == firstDevice)
            {
                FirstDeviceList = firstDeviceScheduler.Session();
            }
            else
            {
                SecondDeviceList = secondDeviceScheduler.Session();
            }
        }
        private void Subscribe(Process p)
        {
            if (p != null)
            {
                p.FreeingAResource += FreeingAResourceEventHandler;
            }
        }
        private void Unsubscribe(Process p)
        {
            if (p != null)
            {
                p.FreeingAResource -= FreeingAResourceEventHandler;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
