namespace ProjectTaskManagement.DTO;

public sealed record TaskDraft(int Project,string Title,string Description,DateTime Deadline,string Priority,IReadOnlyList<int> Members);
