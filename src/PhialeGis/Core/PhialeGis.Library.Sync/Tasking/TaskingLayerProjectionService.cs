using System;
using System.Collections.Generic;
using PhialeGis.Library.Domain.Map;
using PhialeGis.Library.Domain.Tasking;

namespace PhialeGis.Library.Sync.Tasking
{
    public sealed class TaskingLayerProjectionService
    {
        public PhLayer BuildLayer(IEnumerable<TaskItem> tasks, string layerName = "Tasking")
        {
            var layer = new PhLayer(string.IsNullOrWhiteSpace(layerName) ? "Tasking" : layerName, PhLayerType.Memory);
            if (tasks == null)
            {
                return layer;
            }

            long featureId = 1L;
            foreach (var task in tasks)
            {
                if (task == null || task.Location == null || task.Location.Geometry == null)
                {
                    continue;
                }

                var feature = new PhialeGis.Library.Geometry.Features.PhFeature(featureId++, task.Location.Geometry);
                feature.Attributes["taskId"] = task.Id;
                feature.Attributes["title"] = task.Title;
                feature.Attributes["status"] = task.Status.ToString();
                feature.Attributes["priority"] = task.Priority.ToString();

                if (task.SystemLink != null)
                {
                    feature.Attributes["providerId"] = task.SystemLink.ProviderId;
                    feature.Attributes["nativeId"] = task.SystemLink.NativeId;
                }

                if (!string.IsNullOrWhiteSpace(task.Location.LayerName))
                {
                    feature.Attributes["sourceLayer"] = task.Location.LayerName;
                }

                if (task.Location.FeatureId.HasValue)
                {
                    feature.Attributes["sourceFeatureId"] = task.Location.FeatureId.Value;
                }

                if (task.Assignee != null && !string.IsNullOrWhiteSpace(task.Assignee.DisplayName))
                {
                    feature.Attributes["assignee"] = task.Assignee.DisplayName;
                }

                layer.AddFeature(feature);
            }

            return layer;
        }

        public PhLayer AppendLayer(PhGis gis, IEnumerable<TaskItem> tasks, string layerName = "Tasking")
        {
            var layer = BuildLayer(tasks, layerName);
            if (gis != null)
            {
                gis.AddLayer(layer);
            }

            return layer;
        }
    }
}
