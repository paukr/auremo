using System.Collections.Generic;
using System.Text;

namespace Auremo
{
    public class MPDCommandList : MPDSendable
    {
        private Queue<MPDCommand> m_Queue = new Queue<MPDCommand>();

        public MPDCommandList()
        {
        }

        public void Add(MPDCommand command)
        {
            m_Queue.Enqueue(command);
        }

        public bool Nonempty
        {
            get
            {
                return m_Queue.Count > 0;
            }
        }

        public string FullSyntax
        {
            get
            {
                StringBuilder result = new StringBuilder();
                result.Append("command_list_begin\n");

                foreach (MPDCommand command in m_Queue)
                {
                    result.Append(command.FullSyntax);
                }

                result.Append("command_list_end\n");
                return result.ToString();
            }
        }
    }
}
