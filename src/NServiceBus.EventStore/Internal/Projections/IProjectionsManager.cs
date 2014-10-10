﻿using System.Collections.Generic;

namespace NServiceBus.Internal.Projections
{
    public interface IProjectionsManager
    {
        bool Exists(string projectionName);
        string GetQuery(string projectionName);
        void CreateContinuous(string projectionName, string query);
        void UpdateQuery(string projectionName, string newQuery);
        void Delete(string projectionName);
        ProjectionInfo GetStatus(string projectionName);
        IList<ProjectionInfo> List();
        void Stop(string projectionName);
        void Enable(string projectionName);
    }
}