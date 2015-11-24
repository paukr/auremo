using System.Collections.Generic;
using System.Text;

namespace Auremo.Protocol
{
    public class MPDCommandList
    {
        private Queue<MPDCommand> m_Queue = new Queue<MPDCommand>();

        public MPDCommandList()
        {
        }

        public void AddCommand(MPDCommand command)
        {
            m_Queue.Enqueue(command);
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
                    result.Append("\n");
                }

                result.Append("command_list_end\n");
                return result.ToString();
            }
        }
    }
}
