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
        private const string feedUrl = "http://ameshossu.blog58.fc2.com/?xml";
        BlogEntry[] entries;
        int curEntry, curImage;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            button1.Click += new RoutedEventHandler(button1_Click);
            button2.Click += new RoutedEventHandler(button2_Click);

            loadUrl(feedUrl);
        }

        void button2_Click(object sender, RoutedEventArgs e)
        {
            //next
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
            //prev
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
            PageTitle.Text = entries[curEntry].Title;
            image1.Source = new BitmapImage(new Uri(entries[curEntry].ImageUrls[curImage]));
        }

        private void loadUrl(string url)
        {
            var client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadStringCompleted);
            client.DownloadStringAsync(new Uri(url));
        }

        void client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs ev)
        {
            var doc = XDocument.Parse(ev.Result);
            var q = from e in doc.Root.Elements()
                    where e.Name.ToString().EndsWith("item")
                    select new BlogEntry()
                    {
                        Title = e.Element("{http://purl.org/rss/1.0/}title").Value,
                        Link = e.Element("{http://purl.org/rss/1.0/}link").Value,
                        Content = e.Element("{http://purl.org/rss/1.0/modules/content/}encoded").Value,
                    };
            entries = q.ToArray();
            startShow();
        }

    }
}