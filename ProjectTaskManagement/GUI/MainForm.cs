using System.Data;
using Microsoft.Data.SqlClient;
using ProjectTaskManagement.BUS;
using ProjectTaskManagement.DTO;

namespace ProjectTaskManagement.GUI;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        Shown+=(_,_)=>Run(LoadScreen);
    }
    private void Run(Action action)
    {
        try { action(); }
        catch(Exception ex) when(ex is SqlException or ArgumentException or InvalidOperationException or OverflowException or IOException or UnauthorizedAccessException)
        {
            var message=ex is SqlException sql ? sql.Number==547 ? "Dữ liệu đang được sử dụng hoặc mã tham chiếu không tồn tại." : "Không thể thao tác với SQL Server. Kiểm tra kết nối và dữ liệu." : ex.Message;
            MessageBox.Show(this,message,"Thông báo",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
    }
    private void Catalog(object? sender,EventArgs args)=>Run(()=>
    {
        using var dialog=new CatalogForm();
        dialog.ShowDialog(this);
        LoadScreen();
    });


    private readonly TaskBUS service=new();
    private bool loading;
    private int Project=>projects.SelectedValue is int id?id:0;
    private int SelectedTask=>tasks.CurrentRow?.DataBoundItem is DataRowView row?(int)row["MaTask"]:0;
    private sealed record Member(int Id,string Name) { public override string ToString()=>Name; }
    private int[] CheckedMembers()=>members.CheckedItems.Cast<Member>().Select(member=>member.Id).ToArray();

    private void LoadScreen()
    {
        var selected=Project;
        loading=true;
        try
        {
            projects.DisplayMember="TenDuAn";
            projects.ValueMember="MaDuAn";
            projects.DataSource=service.Projects();
            if(selected>0) projects.SelectedValue=selected;
            if(projects.SelectedIndex<0 && projects.Items.Count>0) projects.SelectedIndex=0;
            if(statuses.Items.Count==0)
            {
                statuses.Items.Add("Tất cả trạng thái");
                statuses.Items.AddRange(TaskBUS.States.Cast<object>().ToArray());
                nextStatus.Items.AddRange(TaskBUS.States.Cast<object>().ToArray());
                priorities.Items.AddRange(TaskBUS.Priorities.Cast<object>().ToArray());
                statuses.SelectedIndex=0;
                nextStatus.SelectedIndex=0;
                priorities.SelectedIndex=1;
            }
            members.Items.Clear();
            foreach(DataRow row in service.Members().Rows) members.Items.Add(new Member((int)row["MaThanhVien"],(string)row["HoTen"]));
        }
        finally { loading=false; }
        RefreshProject();
    }

    private void RefreshProject()
    {
        loading=true;
        try
        {
            tasks.DataSource=service.Tasks(Project,statuses.SelectedIndex>0?statuses.Text:"");
            overdue.DataSource=service.Overdue(Project);
            progress.DataSource=service.Progress(Project);
        }
        finally { loading=false; }
        RefreshAssignees();
    }
    private void RefreshAssignees()=>assignees.DataSource=service.Assignees(SelectedTask);
    private void Filter(object? sender,EventArgs args) { if(!loading && IsHandleCreated) Run(RefreshProject); }
    private void SelectTask(object? sender,EventArgs args) { if(!loading && IsHandleCreated) Run(RefreshAssignees); }
    private void CreateTask(object? sender,EventArgs args)=>Run(()=>
    {
        service.Create(new(Project,taskName.Text,description.Text,deadline.Value,priorities.Text,CheckedMembers()));
        taskName.Clear();
        description.Clear();
        RefreshProject();
    });
    private void Assign(object? sender,EventArgs args)=>Run(()=>{service.Assign(SelectedTask,CheckedMembers());RefreshProject();});
    private void ChangeStatus(object? sender,EventArgs args)=>Run(()=>{service.ChangeStatus(SelectedTask,nextStatus.Text);RefreshProject();});

}
