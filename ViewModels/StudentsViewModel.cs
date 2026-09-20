using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using TuitionManagementSystem.Data;
using TuitionManagementSystem.Helpers;
using TuitionManagementSystem.Models;

namespace TuitionManagementSystem.ViewModels
{
    public class StudentsViewModel : ViewModelBase
    {
        public string Grade => AppSession.CurrentGrade;
        public bool IsAdmin => AppSession.CurrentUser?.Role == "Admin";

        public ObservableCollection<Student> Students { get; set; } = new();

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { SetField(ref _searchText, value); Search(); }
        }

        public int EnrolledCount => Students.Count;

        // ---- Add / Edit form fields ----
        private bool _isFormVisible;
        public bool IsFormVisible
        {
            get => _isFormVisible;
            set => SetField(ref _isFormVisible, value);
        }

        private string _formTitle = "Add New Student";
        public string FormTitle
        {
            get => _formTitle;
            set => SetField(ref _formTitle, value);
        }

        private int _editingId;
        private string _name, _school, _parentGuardian, _contact;
        public string Name { get => _name; set => SetField(ref _name, value); }
        public string School { get => _school; set => SetField(ref _school, value); }
        public string ParentGuardian { get => _parentGuardian; set => SetField(ref _parentGuardian, value); }
        public string Contact { get => _contact; set => SetField(ref _contact, value); }

        public RelayCommand AddNewCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand<Student> EditCommand { get; }
        public RelayCommand<Student> DeleteCommand { get; }

        public StudentsViewModel()
        {
            AddNewCommand = new RelayCommand(_ => ShowAddForm());
            SaveCommand = new RelayCommand(_ => SaveStudent());
            CancelCommand = new RelayCommand(_ => IsFormVisible = false);
            EditCommand = new RelayCommand<Student>(EditStudent);
            DeleteCommand = new RelayCommand<Student>(DeleteStudent);

            LoadStudents();
        }

        private void LoadStudents()
        {
            Students.Clear();
            foreach (var s in DatabaseHelper.GetStudentsByGrade(Grade))
                Students.Add(s);
            OnPropertyChanged(nameof(EnrolledCount));
        }

        private void Search()
        {
            Students.Clear();
            var all = DatabaseHelper.GetStudentsByGrade(Grade);
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? all
                : all.Where(s => s.Name.ToLower().Contains(SearchText.ToLower())
                              || s.StudentCode.ToLower().Contains(SearchText.ToLower())).ToList();
            foreach (var s in filtered) Students.Add(s);
            OnPropertyChanged(nameof(EnrolledCount));
        }

        private void ShowAddForm()
        {
            if (!IsAdmin)
            {
                MessageBox.Show("Only an Administrator can add new students.", "Not allowed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _editingId = 0;
            FormTitle = "Add New Student";
            Name = School = ParentGuardian = Contact = "";
            IsFormVisible = true;
        }

        private void EditStudent(Student s)
        {
            if (s == null) return;
            if (!IsAdmin)
            {
                MessageBox.Show("Only an Administrator can edit student records.", "Not allowed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _editingId = s.Id;
            FormTitle = "Edit Student";
            Name = s.Name;
            School = s.School;
            ParentGuardian = s.ParentGuardian;
            Contact = s.Contact;
            IsFormVisible = true;
        }

        private void SaveStudent()
        {
            if (!IsAdmin)
            {
                MessageBox.Show("Only an Administrator can add or edit student records.", "Not allowed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IsFormVisible = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("Please enter the student's name.", "Missing information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_editingId == 0)
            {
                var student = new Student
                {
                    StudentCode = DatabaseHelper.GetNextStudentCode(),
                    Name = Name.Trim(),
                    Grade = Grade,
                    School = School,
                    ParentGuardian = ParentGuardian,
                    Contact = Contact
                };
                DatabaseHelper.AddStudent(student);
            }
            else
            {
                DatabaseHelper.UpdateStudent(new Student
                {
                    Id = _editingId,
                    Name = Name.Trim(),
                    School = School,
                    ParentGuardian = ParentGuardian,
                    Contact = Contact
                });
            }

            IsFormVisible = false;
            LoadStudents();
        }

        private void DeleteStudent(Student s)
        {
            if (s == null) return;
            if (!IsAdmin)
            {
                MessageBox.Show("Only an Administrator can delete student records.", "Not allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Delete {s.Name} ({s.StudentCode})? This cannot be undone.",
                "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                DatabaseHelper.DeleteStudent(s.Id);
                LoadStudents();
            }
        }
    }
}
