using Structures;

namespace CourseWork
{
    class CPUScheduler
    {
        private Resource resource;
        private SortedLinkedList<Process> list;

        public CPUScheduler(Resource resource, SortedLinkedList<Process> list)
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
            newActiveProcess.Status = ProcessStatus.running;
            list.Remove();
            resource.ActiveProcess = newActiveProcess;
            return list;
        }
    }
}