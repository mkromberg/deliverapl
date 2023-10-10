using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Dyalog;
using AplClasses;

namespace WorkerBeesManager {

    public static class WorkerBeesFactory {
        public static AplSession[] GetBees(int numBees) {
            List<AplSession> aplSessions = new List<AplSession>();
            if (numBees == 0) {
                numBees = Environment.ProcessorCount;
            }
            for (int i = 0; i < numBees; i++) {
                var number = i + 1;
                aplSessions.Add(new AplSession());
            }
            return aplSessions.ToArray();
        }

    }

}
