using Microsoft.AspNetCore.Mvc;


namespace WebApplication2.Models
{
    public class ProfileViewModel
    {
        public UserModel User { get; set; } 
        public TeacherProfile TeacherInfo { get; set; } 
    }
}
