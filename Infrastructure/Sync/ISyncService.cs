using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Sync
{
    public interface ISyncService
    {
        Task<SyncResult> SyncAsync(string source, int maxResults = 1000);
        Task<SyncResult> SyncAllAsync(int maxResultsPerSource = 1000);
    }
}
