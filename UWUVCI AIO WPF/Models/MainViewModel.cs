﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UWUVCI_AIO_WPF
{
    class MainViewModel : BaseModel
    {
        //public GameConfig GameConfiguration { get; set; }
        private GameConfig gameConfiguration = new GameConfig();

        public List<string> test { get; set; } = new List<string>();
        public GameConfig GameConfiguration
        {
            get { return gameConfiguration; }
            set
            {
                gameConfiguration = value;
                OnPropertyChanged();
            }
        }
        private string romPath;

        public string RomPath
        {
            get { return romPath; }
            set { romPath = value;
                OnPropertyChanged();
            }
        }

        public bool pathsSet { get; set; } = false;

        public bool PathsSet
        {
            get { return pathsSet; }
            set
            {
                pathsSet = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            test.Add("1");
            test.Add("2");
            test.Add("3");
            test.Add("4");
            test.Add("5");

            GameConfiguration = new GameConfig();
            UpdatePathSet(Properties.Settings.Default.PathsSet);
        }

        public void UpdatePathSet(bool newValue)
        {
            PathsSet = newValue;
        }
    }
}
