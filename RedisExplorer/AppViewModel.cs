﻿using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Windows;

using Caliburn.Micro;

using RedisExplorer.Controls;
using RedisExplorer.Interface;
using RedisExplorer.Models;

using StackExchange.Redis;

namespace RedisExplorer
{
    [Export(typeof(AppViewModel))]
    public sealed class AppViewModel : Conductor<ITabItem>.Collection.OneActive, IApp
    {
        #region Private members

        private readonly IEventAggregator eventAggregator;

        private readonly IWindowManager windowManager;

        private string statusBarTextBlock;

        private BindableCollection<RedisServer> servers; 

        #endregion

        #region Properties

        public BindableCollection<RedisServer> Servers
        {
            get { return servers; }
            set
            {
                servers = value;
                NotifyOfPropertyChange(() => Servers);
            }
        }

        #endregion

        public AppViewModel(IEventAggregator eventAggregator, IWindowManager windowManager)
        {
            this.DisplayName = "Redis Explorer";

            this.eventAggregator = eventAggregator;
            eventAggregator.Subscribe(this);
            this.windowManager = windowManager;
            Servers = new BindableCollection<RedisServer>();

            LoadServers();
        }

        private void LoadServers()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("192.168.1.161,keepAlive = 180,allowAdmin=true");

            foreach(var endpoint in redis.GetEndPoints())
            {
                var server = redis.GetServer(endpoint);
                var svm = new RedisServer(server) { Display = "Redis Server" };

                var info = server.Info("keyspace");
                var databases = server.ConfigGet("databases");
                if (databases != null)
                {
                    int dbcounter = 0;
                    if (int.TryParse(databases.First().Value, out dbcounter))
                    {
                        foreach (var dbnumber in Enumerable.Range(0, dbcounter))
                        {
                            var display = dbnumber.ToString();
                            if (info != null)
                            {
                                var dbinfo = info[0].FirstOrDefault(x => x.Key == "db" + display);
                                if (!string.IsNullOrEmpty(dbinfo.Value))
                                {
                                    display += " (" + dbinfo.Value.Split(',')[0].Split('=')[1] + ")";
                                }
                            }

                            var db = new RedisDatabase(svm, redis.GetDatabase(dbnumber)) { Display = display };

                            svm.Children.Add(db);
                        }
                    }
                }

                Servers.Add(svm);
            }
        }

        #region Properties

        public string StatusBarTextBlock
        {
            get
            {
                return statusBarTextBlock;
            }
            set
            {
                statusBarTextBlock = value;
                NotifyOfPropertyChange(() => StatusBarTextBlock);
            }
        }

        #endregion

        #region Menu

        public void Exit()
        {
            Application.Current.Shutdown();
        }


        public void AddServer()
        {
            dynamic settings = new ExpandoObject();
            settings.Width = 300;
            settings.Height = 250;
            settings.WindowStartupLocation = WindowStartupLocation.Manual;
            settings.Title = "Add Server";

            windowManager.ShowWindow(new AddConnectionViewModel(eventAggregator), null, settings);    
        }

        #endregion
    }
}
