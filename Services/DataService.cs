using LibraryApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryApp.Services;

public class DataService
{
    private LibraryabonementContext NewCtx() => new();

    public async Task<List<Models.Book>> GetBooksAsync()
    {
        await using var ctx = NewCtx();
        return await ctx.Books.AsNoTracking().ToListAsync();
    }
    public async Task<bool> AddBookAsync(Models.Book b)
    {
        await using var ctx = NewCtx();
        ctx.Books.Add(b);
        return await ctx.SaveChangesAsync() > 0;
    }
    public async Task<bool> UpdateBookAsync(Models.Book b)
    {
        await using var ctx = NewCtx();
        ctx.Books.Update(b);
        return await ctx.SaveChangesAsync() > 0;
    }
    public async Task<bool> DeleteBookAsync(string id)
    {
        await using var ctx = NewCtx();
        var e = await ctx.Books.FindAsync(id);
        if (e == null) return false;
        // Каскад: Book -> Copies -> Issues, Book -> BookCatalogs
        var copies = await ctx.Copies.Where(c => c.BookId == id).ToListAsync();
        var invNumbers = copies.Select(c => c.InventoryNumber).ToList();
        var issues = await ctx.Issues.Where(i => i.InventoryNumber != null && invNumbers.Contains(i.InventoryNumber)).ToListAsync();
        var bookCats = await ctx.BookCatalogs.Where(bc => bc.BookId == id).ToListAsync();
        foreach (var i in issues) ctx.Issues.Remove(i);
        foreach (var c in copies) ctx.Copies.Remove(c);
        foreach (var bc in bookCats) ctx.BookCatalogs.Remove(bc);
        ctx.Books.Remove(e);
        return await ctx.SaveChangesAsync() > 0;
    }

    public async Task<List<Models.Reader>> GetReadersAsync()
    {
        await using var ctx = NewCtx();
        return await ctx.Readers.AsNoTracking().ToListAsync();
    }
    public async Task<bool> AddReaderAsync(Models.Reader r)
    {
        await using var ctx = NewCtx();
        ctx.Readers.Add(r);
        return await ctx.SaveChangesAsync() > 0;
    }
    public async Task<bool> UpdateReaderAsync(Models.Reader r)
    {
        await using var ctx = NewCtx();
        ctx.Readers.Update(r);
        return await ctx.SaveChangesAsync() > 0;
    }
    public async Task<bool> DeleteReaderAsync(int id)
    {
        await using var ctx = NewCtx();
        var e = await ctx.Readers.FindAsync(id);
        if (e == null) return false;
        // Каскадное удаление связанных выдач
        var issues = await ctx.Issues.Where(i => i.TicketNumber == id).ToListAsync();
        foreach (var i in issues) ctx.Issues.Remove(i);
        ctx.Readers.Remove(e);
        return await ctx.SaveChangesAsync() > 0;
    }

    public async Task<List<Models.Copy>> GetCopiesAsync()
    {
        await using var ctx = NewCtx();
        return await ctx.Copies.AsNoTracking().ToListAsync();
    }
    public async Task<bool> AddCopyAsync(Models.Copy c)
    {
        await using var ctx = NewCtx();
        ctx.Copies.Add(c);
        return await ctx.SaveChangesAsync() > 0;
    }
    public async Task<bool> UpdateCopyAsync(Models.Copy c)
    {
        await using var ctx = NewCtx();
        ctx.Copies.Update(c);
        return await ctx.SaveChangesAsync() > 0;
    }
    public async Task<bool> DeleteCopyAsync(string inv)
    {
        await using var ctx = NewCtx();
        var e = await ctx.Copies.FindAsync(inv);
        if (e == null) return false;
        var issues = await ctx.Issues.Where(i => i.InventoryNumber == inv).ToListAsync();
        foreach (var i in issues) ctx.Issues.Remove(i);
        ctx.Copies.Remove(e);
        return await ctx.SaveChangesAsync() > 0;
    }

    public async Task<List<Models.ThematicCatalog>> GetThemesAsync()
    {
        await using var ctx = NewCtx();
        return await ctx.ThematicCatalogs.AsNoTracking().ToListAsync();
    }
    public async Task<bool> AddThemeAsync(Models.ThematicCatalog t)
    {
        await using var ctx = NewCtx();
        ctx.ThematicCatalogs.Add(t);
        return await ctx.SaveChangesAsync() > 0;
    }
    public async Task<bool> UpdateThemeAsync(Models.ThematicCatalog t)
    {
        await using var ctx = NewCtx();
        ctx.ThematicCatalogs.Update(t);
        return await ctx.SaveChangesAsync() > 0;
    }
    public async Task<bool> DeleteThemeAsync(int id)
    {
        await using var ctx = NewCtx();
        var e = await ctx.ThematicCatalogs.FindAsync(id);
        if (e == null) return false;
        // Theme -> Copies -> Issues, Theme -> BookCatalogs
        var copies = await ctx.Copies.Where(c => c.ThemeCode == id).ToListAsync();
        var invNumbers = copies.Select(c => c.InventoryNumber).ToList();
        var issues = await ctx.Issues.Where(i => i.InventoryNumber != null && invNumbers.Contains(i.InventoryNumber)).ToListAsync();
        var bookCats = await ctx.BookCatalogs.Where(bc => bc.ThemeCode == id).ToListAsync();
        foreach (var i in issues) ctx.Issues.Remove(i);
        foreach (var c in copies) ctx.Copies.Remove(c);
        foreach (var bc in bookCats) ctx.BookCatalogs.Remove(bc);
        ctx.ThematicCatalogs.Remove(e);
        return await ctx.SaveChangesAsync() > 0;
    }

    public async Task<List<Models.DistributionToReader>> GetLoansAsync()
    {
        await using var ctx = NewCtx();
        return await ctx.Issues.AsNoTracking().ToListAsync();
    }
    public async Task<bool> AddLoanAsync(Models.DistributionToReader l)
    {
        await using var ctx = NewCtx();
        ctx.Issues.Add(l);
        return await ctx.SaveChangesAsync() > 0;
    }
    public async Task<bool> DeleteLoanAsync(int ticket, string inv, System.DateTime? date)
    {
        await using var ctx = NewCtx();
        var e = await ctx.Issues.FirstOrDefaultAsync(x => x.TicketNumber == ticket && x.InventoryNumber == inv && x.IssueDate == date);
        if (e == null) return false;
        ctx.Issues.Remove(e);
        return await ctx.SaveChangesAsync() > 0;
    }
}
