namespace fr.guiet.kquatre.ui.viewmodel
{
    public class ComboBoxItem
    {
        private string _id;
        private string _value;

        public ComboBoxItem(string id, string value)
        {
            _id = id;
            _value = value;
        }

        public string Id
        {
            get
            {
                return _id;
            }
        }

        public string Value
        {
            get
            {
                return _value;
            }
        }
    }
}
