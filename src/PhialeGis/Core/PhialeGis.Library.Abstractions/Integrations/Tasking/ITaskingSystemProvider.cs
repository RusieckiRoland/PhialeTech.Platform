using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhialeGis.Library.Abstractions.Integrations.Tasking
{
    public interface ITaskingSystemProvider
    {
        TaskingProviderDescriptor Descriptor { get; }

        Task<IReadOnlyList<TaskingExternalRecord>> QueryTasksAsync(TaskingExternalQuery query, CancellationToken cancellationToken);

        Task<TaskingExternalRecord> CreateOrUpdateTaskAsync(TaskingExternalRecord task, CancellationToken cancellationToken);

        Task<TaskingExternalRecord> UpdateTaskStatusAsync(string nativeId, TaskingExternalStatus status, CancellationToken cancellationToken);

        Task<TaskingExternalRecord> AssignTaskAsync(string nativeId, TaskingExternalAssignee assignee, CancellationToken cancellationToken);

        Task<bool> DeleteTaskAsync(string nativeId, CancellationToken cancellationToken);
    }
}
