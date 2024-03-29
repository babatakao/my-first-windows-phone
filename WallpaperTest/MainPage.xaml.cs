﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using Microsoft.Phone.Tasks;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input.Touch;

namespace WallpaperTest
{
    class BlogEntry
    {
        private List<string> imageUrls;
        public string Title { get; set; }
        public string Link { get; set; }
        public string Content
        {
            set
            {
                imageUrls = new List<string>();
                foreach (Match match in Regex.Matches(value, "<img src=\"(.*?)\"[^>]*>\\s*(.{5})"))
                {
                    if (match.Groups[2].Value.Contains("</a>"))
                    {
                        continue;
                    }
                    imageUrls.Add(match.Groups[1].Value);
                }
            }

        }
        public string[] ImageUrls
        {
            get
            {
                return imageUrls.ToArray();
            }
        }
    }
    public partial class MainPage : PhoneApplicationPage
    {
        private string[] feedUrls = new string[] {
            "http://ameshossu.blog58.fc2.com/?xml",
            "http://hatchannikki.blog107.fc2.com/?xml"
        };
        BlogEntry[] entries;
        int curBlog, curEntry, curImage;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            button1.Click += new RoutedEventHandler(button1_Click);
            button2.Click += new RoutedEventHandler(button2_Click);
            button3.Click += new RoutedEventHandler(button3_Click);
            PageTitle.Tap += new EventHandler<GestureEventArgs>(PageTitle_Tap);
            image1.Tap += delegate(object sender, GestureEventArgs e) { saveImage(); };

            loadUrl();
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
            timer_Tick(null, null);

            TouchPanel.EnabledGestures = GestureType.Flick;
            image1.ManipulationCompleted += new EventHandler<ManipulationCompletedEventArgs>(image1_ManipulationCompleted);
        }

        void button3_Click(object sender, RoutedEventArgs e)
        {
            if (++curBlog >= feedUrls.Length)
            {
                curBlog = 0;
            }
            loadUrl();
        }

        void image1_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (TouchPanel.IsGestureAvailable)
            {
                var ges = TouchPanel.ReadGesture();
                if (ges.GestureType == GestureType.Flick)
                {
                    if (ges.Delta.X < 0)
                    {
                        next();
                    }
                    else
                    {
                        prev();
                    }
                }
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            PageTitle.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        void saveImage()
        {
            var library = new MediaLibrary();
            var name = entries[curEntry].Title + "-" + DateTime.Now.ToString();
            var url = entries[curEntry].ImageUrls[curImage];
            var client = new WebClient();
            client.OpenReadCompleted += delegate(object sender, OpenReadCompletedEventArgs e) {
                library.SavePicture(name, e.Result);
                MessageBox.Show("saved: " + name);
            };
            client.OpenReadAsync(new Uri(url));
        }

        void PageTitle_Tap(object sender, GestureEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri(entries[curEntry].Link);
            task.Show();
        }

        void button2_Click(object sender, RoutedEventArgs e)
        {
            next();
        }

        void next()
        {
            if (curImage == entries[curEntry].ImageUrls.Length - 1)
            {
                if (curEntry == entries.Length - 1)
                {
                    return;
                }
                curImage = 0;
                curEntry++;
            }
            else
            {
                curImage++;
            }
            showImage();
        }

        void button1_Click(object sender, RoutedEventArgs e)
        {
            prev();
        }
        void prev()
        {
            if (curImage == 0)
            {
                if (curEntry == 0)
                {
                    return;
                }
                curEntry--;
                curImage = entries[curEntry].ImageUrls.Length - 1;
            }
            else
            {
                curImage--;
            }
            showImage();
        }

        void startShow()
        {
            curEntry = curImage = 0;
            showImage();
        }

        void showImage()
        {
            ApplicationTitle.Text = entries[curEntry].Title;
            image1.Source = new BitmapImage(new Uri(entries[curEntry].ImageUrls[curImage]));
        }

        private void loadUrl()
        {
            var client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadStringCompleted);
            client.DownloadStringAsync(new Uri(feedUrls[curBlog]));
        }

        void client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs ev)
        {
            var doc = XDocument.Parse(ev.Result);
            var q = from entry in
                    from e in doc.Root.Elements()
                    where e.Name.ToString().EndsWith("item")
                    select new BlogEntry()
                    {
                        Title = e.Element("{http://purl.org/rss/1.0/}title").Value,
                        Link = e.Element("{http://purl.org/rss/1.0/}link").Value,
                        Content = e.Element("{http://purl.org/rss/1.0/modules/content/}encoded").Value,
                    }
                where entry.ImageUrls.Count() > 0
                select entry;
            entries = q.ToArray();
            startShow();
        }
    }
}