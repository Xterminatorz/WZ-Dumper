using System;
using System.Windows.Forms;

namespace WzDumper {
	/// <summary>
	/// Provides a Sate Text property for a ToolStripStatusLabel
	/// </summary>
	public class SafeToolStripLabel : ToolStripStatusLabel {
		public override string Text {
			get {
				if ((Parent != null) && // Make sure that the container is already built
				    (Parent.InvokeRequired)) // Is Invoke required?
				{
					GetString getTextDel = () => Text;
					string text = String.Empty;
					try {
						// Invoke the SetText operation from the Parent of the ToolStripStatusLabel
						text = (string) Parent.Invoke(getTextDel, null);
					} catch {
					}

					return text;
				}
				return base.Text;
			}

			set {
				// Get from the container if Invoke is required
				if ((Parent != null) && // Make sure that the container is already built
				    (Parent.InvokeRequired)) // Is Invoke required?
				{
					SetText setTextDel = delegate(string text) {
					                     	Text = text;
					                     };

					try {
						// Invoke the SetText operation from the Parent of the ToolStripStatusLabel
						Parent.Invoke(setTextDel, new object[] {value});
					} catch {
					}
				} else
					base.Text = value;
			}
		}

		#region Nested type: GetString

		private delegate string GetString();

		#endregion

		#region Nested type: SetText

		private delegate void SetText(string text);

		#endregion
	}
}