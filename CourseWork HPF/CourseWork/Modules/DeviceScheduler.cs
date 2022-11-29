using Structures;

namespace CourseWork
{
    class DeviceScheduler
    {
        private Resource resource;
        private SortedLinkedList<Process> list;

        public DeviceScheduler(Resource resource, SortedLinkedList<Process> list)
        {
            this.resource = resource;
            this.list = list;
        }
        public SortedLinkedList<Process> Session()
        {
            if (list.Count == 0)
            {
                return list;
            }
            Process newActiveProcess = list.Item();
            list.Remove();
            resource.ActiveProcess = newActiveProcess;
            return list;
        }
    }
}

