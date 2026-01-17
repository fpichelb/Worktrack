namespace Worktrack.ViewModels;

// ViewModels 
public class ActivityListItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Ort { get; set; } = "";
    public DateTime Datum { get; set; }
    public int MaxTeilnehmer { get; set; }
    public int EmpfohleneTeilnehmer { get; set; }
    public int ActiveCount { get; set; }
}
    public class ActivityDetailVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Ort { get; set; } = "";
        public string Info { get; set; } = "";
        public DateTime Datum { get; set; }
        public int MaxTeilnehmer { get; set; }
        public int EmpfohleneTeilnehmer { get; set; }
        public int ActiveCount { get; set; }
        public DateTime? LastUnregistrationAt { get; set; }
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";   // "image/jpeg", "image/png"...
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public bool IsRegistered { get; set; }
        public List<ParticipantVm> Participants { get; set; } = new();
    }

    public class ParticipantVm
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; } = "";
        public DateTime RegisteredAt { get; set; }
    }

    public class ActivityCreateVm
    {
        public string Name { get; set; } = "";
        public string Ort { get; set; } = "";
        public string Info { get; set; } = "";
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";   // "image/jpeg", "image/png"...
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public DateTime Datum { get; set; }
        public DateTime UnregisteredAt {get;set;}
        public int MaxTeilnehmer { get; set; } = 0;
        public int EmpfohleneTeilnehmer { get; set; } = 0;
    }