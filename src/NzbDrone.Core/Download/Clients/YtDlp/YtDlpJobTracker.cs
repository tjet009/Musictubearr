using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Download.Clients.YtDlp
{
    public interface IYtDlpJobTracker
    {
        void Add(YtDlpJob job);
        void Update(YtDlpJob job);
        bool Remove(string downloadId);
        YtDlpJob Get(string downloadId);
        IEnumerable<YtDlpJob> GetAll();
    }

    public class YtDlpJob
    {
        public string DownloadId { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string OutputPath { get; set; }
        public DownloadItemStatus Status { get; set; }
        public string Message { get; set; }
        public long TotalSize { get; set; }
        public DateTime Started { get; set; }
        public DateTime? Completed { get; set; }
        public int DefinitionId { get; set; }
    }

    public class YtDlpJobTracker : IYtDlpJobTracker
    {
        private readonly ConcurrentDictionary<string, YtDlpJob> _jobs = new ConcurrentDictionary<string, YtDlpJob>();

        public void Add(YtDlpJob job)
        {
            _jobs[job.DownloadId] = job;
        }

        public void Update(YtDlpJob job)
        {
            _jobs[job.DownloadId] = job;
        }

        public bool Remove(string downloadId)
        {
            return _jobs.TryRemove(downloadId, out _);
        }

        public YtDlpJob Get(string downloadId)
        {
            _jobs.TryGetValue(downloadId, out var job);
            return job;
        }

        public IEnumerable<YtDlpJob> GetAll()
        {
            return _jobs.Values.OrderByDescending(j => j.Started).ToList();
        }
    }
}
