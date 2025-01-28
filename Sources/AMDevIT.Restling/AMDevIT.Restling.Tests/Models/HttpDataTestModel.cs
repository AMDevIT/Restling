namespace AMDevIT.Restling.Tests.Models
{
    public class HttpDataTestModel()
    {
        #region Properties

        public string? Name
        {
            get;
            set;
        }

        public string? Surname
        {
            get;
            set;
        }

        #endregion

        #region .ctor

        public HttpDataTestModel(string name,
                            string surname)
            : this()
        {
            this.Name = name;
            this.Surname = surname;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"Name: {this.Name}, Surname: {this.Surname}";
        }

        #endregion
    }
}
