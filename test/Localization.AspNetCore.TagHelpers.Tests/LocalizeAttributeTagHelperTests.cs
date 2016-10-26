//-----------------------------------------------------------------------
// <copyright file="LocalizeAttributeTagHelperTests.cs">
//   Copyright (c) Kim Nordmo. All rights reserved.
//   Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// <author>Kim Nordmo</author>
//-----------------------------------------------------------------------

namespace Localization.AspNetCore.TagHelpers.Tests
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Text;
  using System.Text.Encodings.Web;
  using System.Threading.Tasks;
  using Microsoft.AspNetCore.Hosting;
  using Microsoft.AspNetCore.Mvc.Localization;
  using Microsoft.AspNetCore.Mvc.Rendering;
  using Microsoft.AspNetCore.Mvc.ViewEngines;
  using Microsoft.AspNetCore.Razor.TagHelpers;
  using Microsoft.Extensions.Localization;
  using Moq;
  using NUnit.Framework;

  public class LocalizeAttributeTagHelperTests
  {
    private Mock<IHostingEnvironment> hostingMock;
    private Mock<IHtmlLocalizerFactory> locFactoryMock;
    private Mock<IHtmlLocalizer> locMock;

    #region Setup/Teardown

    [SetUp]
    public void Reset()
    {
      locMock.Reset();
      locMock.Setup(x => x.GetString(It.IsAny<string>())).Returns<string>(s => new LocalizedString(s, s, true));
      locMock.Setup(x => x[It.IsAny<string>()]).Returns<string>(s => new LocalizedHtmlString(s, s, true));
    }

    [OneTimeSetUp]
    public void Setup()
    {
      locMock = new Mock<IHtmlLocalizer>();
      hostingMock = new Mock<IHostingEnvironment>();
      hostingMock.Setup(x => x.ApplicationName).Returns("Localization.AspNetCore.TagHelpers.Tests");
      locFactoryMock = new Mock<IHtmlLocalizerFactory>();
      locFactoryMock.Setup(x => x.Create(It.IsAny<string>(), hostingMock.Object.ApplicationName)).Returns(locMock.Object);
    }

    #endregion Setup/Teardown

    #region Constructor

    [Test]
    public void Constructor_ThrowsArgumentNullExceptionIfPassedIViewLocalizerIsNull()
    {
      Assert.That(() => new LocalizeAttributeTagHelper(null, new Mock<IHostingEnvironment>().Object), Throws.ArgumentNullException.And.Message.Contains("localizer"));
    }

    #endregion Constructor

    #region Init

    [TestCase("TestApplication", "Views/Home/Index.cshtml", "Views/Home/Index.cshtml", "TestApplication.Views.Home.Index")]
    [TestCase("TestApplication", "/Views/Home/Index.cshtml", "/Views/Home/Index.cshtml", "TestApplication.Views.Home.Index")]
    [TestCase("TestApplication", "\\Views\\Home\\Index.cshtml", "\\Views\\Home\\Index.cshtml", "TestApplication.Views.Home.Index")]
    [TestCase("TestApplication.Web", "Views/Home/Index.cshtml", "Views/Home/Index.cshtml", "TestApplication.Web.Views.Home.Index")]
    [TestCase("TestApplication", "Views/Home/Index.cshtml", "Views/Shared/_Layout.cshtml", "TestApplication.Views.Shared._Layout")]
    [TestCase("TestApplication", "Views/Home/Index.cshtml", "Views/Shared/_MyPartial.cshtml", "TestApplication.Views.Shared._MyPartial")]
    [TestCase("TestApplication", "Views/Home/Index.cshtml", "Views/Home/_HomePartial.cshtml", "TestApplication.Views.Home._HomePartial")]
    [TestCase("TestApplication", "Views/Home/Index.cshtml", null, "TestApplication.Views.Home.Index")]
    [TestCase("TestApplication", "Views/Home/Index.txt", null, "TestApplication.Views.Home.Index")]
    [TestCase("TestApplication", "Views/Home/Index.cshtml", "", "TestApplication.Views.Home.Index")]
    [TestCase("TestApplication", "Views/Home/Index.txt", "", "TestApplication.Views.Home.Index")]
    public void Init_CreatesHtmlLocalizerFromViewContext(string appName, string viewPath, string executionPath, string expectedBaseName)
    {
      var hostingEnvironment = new Mock<IHostingEnvironment>();
      hostingEnvironment.Setup(a => a.ApplicationName).Returns(appName);
      var factoryMock = TestHelper.CreateFactoryMock(true);
      var view = new Mock<IView>();
      view.Setup(v => v.Path).Returns(viewPath);
      var viewContext = new ViewContext();
      viewContext.ExecutingFilePath = executionPath;
      viewContext.View = view.Object;
      var tagHelper = new LocalizeAttributeTagHelper(factoryMock.Object, hostingEnvironment.Object);
      tagHelper.ViewContext = viewContext;
      var context = TestHelper.CreateTagContext();

      tagHelper.Init(context);

      factoryMock.Verify(x => x.Create(expectedBaseName, appName), Times.Once());
    }

    #endregion Init

    #region Process

    [Test]
    public void Process_CanLocalizeMultipleAttributes()
    {
      var tagHelper = InitTagHelper();
      var context = CreateTagContext();
      var output = CreateTagOutput("span", "Oh Yeah");
      tagHelper.AttributeValues = new Dictionary<string, string>
      {
        { "title", "Localize Me" },
        { "alt", "Me too" }
      };
      locMock.Setup(x => x.GetString("Localize Me")).Returns<string>(x => new LocalizedString(x, "I was localized"));
      locMock.Setup(x => x.GetString("Me too")).Returns<string>(x => new LocalizedString(x, "I was also localized"));
      var expected = "<span title=\"I was localized\" alt=\"I was also localized\">Oh Yeah</span>";

      var actual = CreateHtmlOutput(tagHelper, context, output);

      Assert.That(output.Attributes.ContainsName("title"), Is.True, "Title attribute has not been set");
      Assert.That(output.Attributes.ContainsName("alt"), Is.True, "Alt attribute has not been set");
      Assert.That(actual, Is.EqualTo(expected));

      locMock.Verify(x => x.GetString(It.IsAny<string>()), Times.Exactly(2));
    }

    [Test]
    public void Process_CanLocalizeSingleAttributeValue()
    {
      var tagHelper = InitTagHelper();
      var context = CreateTagContext();
      var output = CreateTagOutput("span", "Does not matter");
      tagHelper.AttributeValues.Add("title", "Localize-me");
      locMock.Setup(x => x.GetString("Localize-me")).Returns<string>(x => new LocalizedString(x, "I Am Localized", false));
      var expected = "<span title=\"I Am Localized\">Does not matter</span>";

      var actual = CreateHtmlOutput(tagHelper, context, output);

      Assert.That(output.Attributes.ContainsName("title"), Is.True, "Title attribute is not set");
      Assert.That(actual, Is.EqualTo(expected));

      locMock.Verify(x => x.GetString("Localize-me"), Times.Once());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("           ")]
    public void Process_EmptyAttributesIsIgnored(string value)
    {
      var tagHelper = InitTagHelper();
      var context = CreateTagContext();
      var output = CreateTagOutput("span", "Yup I'm still here");
      tagHelper.AttributeValues.Add("title", value);
      var expected = "<span>Yup I&#x27;m still here</span>";

      var actual = CreateHtmlOutput(tagHelper, context, output);

      Assert.That(output.Attributes.ContainsName("title"), Is.False, "Title attribute has been set");
      Assert.That(actual, Is.EqualTo(expected));

      locMock.Verify(x => x.GetString(value), Times.Never());
    }

    #endregion Process

    private string CreateHtmlOutput(LocalizeAttributeTagHelper tagHelper, TagHelperContext tagContext, TagHelperOutput tagOutput)
    {
      tagHelper.Init(tagContext);
      var sb = new StringBuilder();

      tagHelper.Process(tagContext, tagOutput);
      var contentTask = tagOutput.GetChildContentAsync();
      contentTask.Wait();
      tagOutput.Content = contentTask.Result;

      using (var writer = new StringWriter(sb))
      {
        tagOutput.WriteTo(writer, HtmlEncoder.Default);
      }

      return sb.ToString();
    }

    private TagHelperContext CreateTagContext(params TagHelperAttribute[] attributes)
    {
      return new TagHelperContext(
        new TagHelperAttributeList(attributes),
        new Dictionary<object, object>(),
        Guid.NewGuid().ToString());
    }

    private TagHelperOutput CreateTagOutput(string name, string content, params TagHelperAttribute[] attributes)
    {
      return new TagHelperOutput(
        name,
        new TagHelperAttributeList(attributes),
        (useCachedResult, encoder) =>
        {
          var tagHelperContent = new DefaultTagHelperContent();
          tagHelperContent.SetContent(content);
          return Task.FromResult<TagHelperContent>(tagHelperContent);
        });
    }

    private LocalizeAttributeTagHelper InitTagHelper()
    {
      return new LocalizeAttributeTagHelper(locFactoryMock.Object, hostingMock.Object)
      {
        ViewContext = new ViewContext()
        {
          ExecutingFilePath = "/Views/Home/Index.cshtml"
        }
      };
    }
  }
}
