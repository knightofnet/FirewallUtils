using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AryxDevLibrary.extensions;
using PocFwIpApp.constant;
using PocFwIpApp.dto;
using PocFwIpApp.utils;

namespace PocFwIpApp.business
{
    class EventsCollector
    {
        public String AppName { get; set; }

        public bool PlaySoundIfNew { get; set; }

        // public DirectionProtocolAdressMap DpaMap { get; set; }
        public Func<EventLogEntry, String> ReportFilter { get; internal set; }

        private EventLog journal = new EventLog("Security");

        public ConcurrentBag<EventLogEntry> Entries { get; private set; }

        private ConcurrentQueue<EventLogEntry> queueToAddMap;
        private HashSet<String> _setReportedEntry;


        public EventsCollector()
        {
            Entries = new ConcurrentBag<EventLogEntry>();
        }

        public bool FilterEntry(EventLogEntry entry, String exeName)
        {
            if (entry.InstanceId != 5157 && entry.InstanceId != 5152)
            {
                return false;
            }

            DateTime dtMin = DateTime.Now.AddHours(-1);
            if (entry.TimeGenerated.IsBefore(dtMin))
            {
                return false;
            }

            if (entry.GetDirection() != DirectionsEnum.Outbound)
            {
                return false;
            }

            if (!entry.ReplacementStrings[1].EndsWith(exeName, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            return true;
        }



        public void DoLoopCollect(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgWorker = sender as BackgroundWorker;
            _setReportedEntry = new HashSet<String>();

            queueToAddMap = new ConcurrentQueue<EventLogEntry>();

            var entries = journal.Entries.Cast<EventLogEntry>().Where(r => FilterEntry(r, AppName)).ToList();

            foreach (EventLogEntry entry in entries)
            {
                Entries.Add(entry);
                ReportProgress(entry, bgWorker);
            }
            if (PlaySoundIfNew && entries.Any()) System.Media.SystemSounds.Beep.Play();

            journal.EnableRaisingEvents = true;
            journal.EntryWritten += JournalOnEntryWritten;


            while (!bgWorker.CancellationPending)
            {
                bool haveDequeudAny = false;
                EventLogEntry entryDequeued;
                while (queueToAddMap.TryDequeue(out entryDequeued))
                {
                    Entries.Add(entryDequeued);
                    ReportProgress(entryDequeued, bgWorker);
                    haveDequeudAny = true;
                    if (bgWorker.CancellationPending)
                    {
                        break;
                    }
                }

                Thread.Sleep(250);
                if (PlaySoundIfNew && haveDequeudAny) System.Media.SystemSounds.Beep.Play();

            }

            journal.EnableRaisingEvents = false;
        }


        private void ReportProgress(EventLogEntry entry, BackgroundWorker bgWorker)
        {
            String key = String.Format("{0}#{1}#{2}", entry.GetDirection(), entry.GetProtocole(),
                entry.GetRemoteAddress());

            if (_setReportedEntry.Contains(key))
            {
                return;
            }
            bgWorker.ReportProgress(1, entry);
            _setReportedEntry.Add(key);
        }

        private void JournalOnEntryWritten(object sender, EntryWrittenEventArgs e)
        {
            if (FilterEntry(e.Entry, AppName))
            {
                queueToAddMap.Enqueue(e.Entry);
            }
        }


    }
}
