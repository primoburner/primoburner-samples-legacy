using System;

namespace AudioBurner.NET
{
	class ListItem
	{
		public object Value;
		public string Description;

		public ListItem(object nvalue, string description)
		{
			Value = nvalue;
			Description = description;
		}

		public override string ToString()
		{
			return Description;
		}
	}

}
