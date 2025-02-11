﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using System.Linq;

namespace Configuration.OptionsPages
{
	[Guid("B0002DC2-56EE-4931-93F7-70D6E9863940")]
	public class SandoDialogPage : DialogPage
	{
		#region Properties
		/// <summary> 
		/// Gets the window an instance of DialogPage that it uses as its user interface. 
		/// </summary> 
		/// <devdoc> 
		/// The window this dialog page will use for its UI. 
		/// This window handle must be constant, so if you are 
		/// returning a Windows Forms control you must make sure 
		/// it does not recreate its handle.  If the window object 
		/// implements IComponent it will be sited by the  
		/// dialog page so it can get access to global services. 
		/// </devdoc> 
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected override IWin32Window Window
		{
			get
			{
				SandoOptionsControl optionsControl = new SandoOptionsControl();
				optionsControl.Location = new Point(0, 0);
				optionsControl.OptionsPage = this;
				optionsControl.NumberOfSearchResultsReturned = this.NumberOfSearchResultsReturned;
                bool allowCollection = true;
                if (!string.IsNullOrEmpty(this.AllowDataCollectionLogging) && bool.TryParse(this.AllowDataCollectionLogging, out allowCollection))
                {
                    optionsControl.AllowDataCollectionLogging = allowCollection;
                }
                else
                {
                    optionsControl.AllowDataCollectionLogging = true;
                }                
                optionsControl.FileExtensionsList = FileExtensionsToIndexList;
				return optionsControl;
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public string NumberOfSearchResultsReturned { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public string AllowDataCollectionLogging { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string _serializedFileExtensions { get; set; }
        public List<string> FileExtensionsToIndexList {
            get
            {
                if (!String.IsNullOrEmpty(_serializedFileExtensions))
                {
                    var splitExtensions = _serializedFileExtensions.Split(new char[] { ';' },
                                                                StringSplitOptions.RemoveEmptyEntries);
                    return splitExtensions.ToList();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                var combinedExtensions = string.Empty;
                if (value != null && value.Any())
                {
                    foreach (var extension in value)
                    {
                        combinedExtensions += extension + ";";
                    }
                }
                _serializedFileExtensions = combinedExtensions;
            }
        }

		#endregion Properties 
	}
}
