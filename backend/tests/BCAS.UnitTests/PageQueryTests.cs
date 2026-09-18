using BCAS.Application.Common;
using Xunit;

namespace BCAS.UnitTests;

public class PageQueryTests
{
    [Fact]
    public void Page_below_one_falls_back_to_the_first_page()
    {
        var query = new PageQuery { Page = 0 };

        Assert.Equal(1, query.Page);
        Assert.Equal(0, query.Offset);
    }

    [Fact]
    public void Page_size_is_capped_so_a_caller_cannot_pull_the_whole_table()
    {
        var query = new PageQuery { PageSize = 5000 };

        Assert.Equal(100, query.PageSize);
    }

    [Fact]
    public void Offset_skips_the_previous_pages()
    {
        var query = new PageQuery { Page = 3, PageSize = 20 };

        Assert.Equal(40, query.Offset);
    }
}
