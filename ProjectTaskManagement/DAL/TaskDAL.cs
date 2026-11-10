using System.Data;
using Microsoft.Data.SqlClient;
using ProjectTaskManagement.DTO;

namespace ProjectTaskManagement.DAL;

internal sealed class TaskDAL
{
    internal DataTable Projects()=>DatabaseHelper.Query("SELECT MaDuAn,TenDuAn FROM DuAn ORDER BY MaDuAn");
    internal DataTable Members()=>DatabaseHelper.Query("SELECT MaThanhVien,HoTen FROM ThanhVien ORDER BY MaThanhVien");
    internal DataTable Tasks(int project,string status)=>DatabaseHelper.Query("""
    SELECT MaTask,TenTask,MoTa,HanNop,DoUuTien,TrangThai
    FROM CongViec WHERE MaDuAn=@project AND (@status=N'' OR TrangThai=@status) ORDER BY MaTask
    """,Db.Int("@project",project),Db.Text("@status",status,50));
    internal DataTable Assignees(int task)=>DatabaseHelper.Query("""
    SELECT v.MaThanhVien,v.HoTen,p.NgayGiao FROM PhanCong p JOIN ThanhVien v ON v.MaThanhVien=p.MaThanhVien
    WHERE p.MaTask=@task ORDER BY v.MaThanhVien
    """,Db.Int("@task",task));

    internal int Create(TaskDraft draft)=>Db.Transaction((c,t)=>
    {
        var id=(int)Db.Scalar(c,t,"""
        INSERT CongViec(MaDuAn,TenTask,MoTa,HanNop,DoUuTien,TrangThai)
        OUTPUT INSERTED.MaTask VALUES(@project,@name,@description,@deadline,@priority,N'To Do')
        """,Db.Int("@project",draft.Project),Db.Text("@name",draft.Title,200),Db.Text("@description",draft.Description),
        Db.Date("@deadline",draft.Deadline),Db.Text("@priority",draft.Priority,20))!;
        Assign(c,t,id,draft.Members);
        return id;
    });

    private static void Assign(SqlConnection c,SqlTransaction t,int task,IReadOnlyList<int> members)
    {
        foreach(var member in members.OrderBy(id=>id))
            Db.Execute(c,t,"""
            IF NOT EXISTS(SELECT 1 FROM PhanCong WITH(UPDLOCK,HOLDLOCK) WHERE MaTask=@task AND MaThanhVien=@member)
            INSERT PhanCong(MaTask,MaThanhVien) VALUES(@task,@member)
            """,Db.Int("@task",task),Db.Int("@member",member));
    }
    internal void Assign(int task,IReadOnlyList<int> members)=>Db.Transaction((c,t)=>{Assign(c,t,task,members);return 0;});

    internal void ChangeStatus(int task,string status)
    {
        if(DatabaseHelper.Execute("UPDATE CongViec SET TrangThai=@state WHERE MaTask=@task",
            Db.Text("@state",status,50),Db.Int("@task",task))!=1) throw new InvalidOperationException("Không tìm thấy công việc.");
    }

    internal DataTable Overdue(int project)
    {
        using var connection=DatabaseHelper.Open();
        using var command=new SqlCommand("""
        SELECT MaTask,TenTask,HanNop,DoUuTien,TrangThai FROM CongViec
        WHERE MaDuAn=@project AND HanNop<GETDATE() AND TrangThai<>N'Done' ORDER BY HanNop,MaTask
        """,connection);
        command.Parameters.Add(Db.Int("@project",project));
        using var reader=command.ExecuteReader();
        var result=new DataTable();
        result.Load(reader);
        return result;
    }

    internal DataTable Progress(int project)=>DatabaseHelper.Query("""
    SELECT v.MaThanhVien,v.HoTen,COUNT(*) AS SoViecDuocGiao,
        SUM(CASE WHEN c.TrangThai=N'Done' THEN 1 ELSE 0 END) AS SoViecHoanThanh,
        CAST(100.0*SUM(CASE WHEN c.TrangThai=N'Done' THEN 1 ELSE 0 END)/COUNT(*) AS decimal(5,2)) AS TyLePhanTram
    FROM CongViec c JOIN PhanCong p ON p.MaTask=c.MaTask JOIN ThanhVien v ON v.MaThanhVien=p.MaThanhVien
    WHERE c.MaDuAn=@project GROUP BY v.MaThanhVien,v.HoTen ORDER BY v.MaThanhVien
    """,Db.Int("@project",project));
}
