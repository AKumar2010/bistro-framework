﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BistroModel
{
	/// <summary>
	/// A resource marked with this attribute will be populated
	/// from a cookie of the same name. If needed, use the Name
	/// property to force a match.
	/// </summary>
	public class CookieReadAttribute : AbstractResourceAttribute
	{
	}
}
