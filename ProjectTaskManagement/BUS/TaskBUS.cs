using System.Data;
using ProjectTaskManagement.DAL;
using ProjectTaskManagement.DTO;

namespace ProjectTaskManagement.BUS;

public sealed class TaskBUS
{
    public static IReadOnlyList<string> States { get; }=Array.AsReadOnly(new[]{"To Do","In Progress","Review","Done"});
    public static IReadOnlyList<string> Priorities { get; }=Array.AsReadOnly(new[]{"Cao","Trung bình","Thấp"});
    private readonly TaskDAL dal=new();
    public DataTable Projects()=>dal.Projects();
    public DataTable Members()=>dal.Members();
    public DataTable Tasks(int project,string status)=>dal.Tasks(project,status);
    public DataTable Assignees(int task)=>dal.Assignees(task);
    public DataTable Overdue(int project)=>dal.Overdue(project);
    public DataTable Progress(int project)=>dal.Progress(project);

    public int Create(TaskDraft draft)
    {
        var title=draft.Title.Trim();
        if(draft.Project<=0 || title.Length is 0 or >200 || !Priorities.Contains(draft.Priority))
            throw new ArgumentException("Chọn dự án, nhập tên công việc và độ ưu tiên hợp lệ.");
        return dal.Create(draft with {Title=title,Members=draft.Members.Distinct().ToArray()});
    }
    public void Assign(int task,IReadOnlyList<int> members)
    {
        if(task<=0 || members.Count==0) throw new ArgumentException("Chọn công việc và ít nhất một thành viên.");
        dal.Assign(task,members.Distinct().ToArray());
    }
    public void ChangeStatus(int task,string status)
    {
        if(task<=0 || !States.Contains(status)) throw new ArgumentException("Chọn công việc và trạng thái hợp lệ.");
        dal.ChangeStatus(task,status);
    }
}
