using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2pChat
{
    class CommunicationData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string recentMessage;

        public string RecentMessage
        {
            get => recentMessage;
            set
            {
                if (value == recentMessage) return;
                recentMessage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecentMessage)));
            }
        }

        private string mainCommand;

        public string MainCommand
        {
            get => mainCommand;
            set
            {
                if (value == mainCommand) return;
                mainCommand = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainCommand)));
            }
        }
    }
}
