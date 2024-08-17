using MickeyUtilityG.Models;
using MickeyUtilityG.Services.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MickeyUtilityG.Services.PageServices
{
    public class TodoListService
    {
        private readonly GoogleSheetsApiService _sheetsApiService;
        private readonly ILogger<TodoListService> _logger;
        private readonly string SPREADSHEET_ID;
        private const string RANGE_NAME = "Sheet1!A:L";

        public TodoListService(GoogleSheetsApiService sheetsApiService, IConfiguration configuration, ILogger<TodoListService> logger)
        {
            _sheetsApiService = sheetsApiService;
            _logger = logger;

            _logger.LogInformation("Initializing TodoListService");
            SPREADSHEET_ID = configuration["Google:SpreadsheetId"];
            _logger.LogInformation($"TodoListService initialized with Spreadsheet ID: {SPREADSHEET_ID}");
        }

        public async Task<List<TodoItem>> GetTodoListFromGoogleSheets()
        {
            try
            {
                _logger.LogInformation("Starting to fetch todo list from Google Sheets");
                var sheetData = await _sheetsApiService.GetValuesAsync(SPREADSHEET_ID, RANGE_NAME);
                _logger.LogInformation($"Retrieved {sheetData.Count} rows from Google Sheets");

                var records = new List<TodoItem>();

                if (sheetData.Count > 0)
                {
                    // Log the first row (headers)
                    _logger.LogInformation($"Headers: {string.Join(", ", sheetData[0])}");

                    for (int row = 1; row < sheetData.Count; row++)
                    {
                        var rowData = sheetData[row];
                        _logger.LogInformation($"Row {row} data: {string.Join(", ", rowData)}");

                        if (rowData.Count < 11)
                        {
                            _logger.LogWarning($"Row {row} has insufficient data: {rowData.Count} columns");
                            continue;
                        }

                        var item = new TodoItem
                        {
                            ID = rowData[0]?.ToString(),
                            Title = rowData[1]?.ToString(),
                            Description = rowData[2]?.ToString(),
                            DueDate = DateTimeOffset.TryParse(rowData[3]?.ToString(), out var dueDate) ? dueDate : (DateTimeOffset?)null,
                            IsCompleted = bool.TryParse(rowData[4]?.ToString(), out var isCompleted) ? isCompleted : false,
                            Category = rowData[5]?.ToString() ?? "Uncategorized",
                            ParentTaskId = rowData[6]?.ToString(),
                            CreatedAt = DateTimeOffset.TryParse(rowData[7]?.ToString(), out var createdAt) ? createdAt : DateTimeOffset.Now,
                            UpdatedAt = DateTimeOffset.TryParse(rowData[8]?.ToString(), out var updatedAt) ? updatedAt : DateTimeOffset.Now,
                            IsDeleted = bool.TryParse(rowData[9]?.ToString(), out var isDeleted) ? isDeleted : false,
                            LastModifiedDate = DateTimeOffset.TryParse(rowData[10]?.ToString(), out var lastModifiedDate) ? lastModifiedDate : DateTimeOffset.Now,
                        };

                        if (rowData.Count > 11)
                        {
                            item.DeletedDate = DateTimeOffset.TryParse(rowData[11]?.ToString(), out var deletedDate) ? deletedDate : (DateTimeOffset?)null;
                        }

                        if (!string.IsNullOrWhiteSpace(item.Title))
                        {
                            records.Add(item);
                            _logger.LogDebug($"Added todo item: {item.Title}");
                        }
                        else
                        {
                            _logger.LogWarning($"Skipped row {row} due to empty title");
                        }
                    }
                }

                _logger.LogInformation($"Processed {records.Count} todo items");
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from Google Sheets");
                throw;
            }
        }

        public async Task<List<TodoItem>> UpdateTodoListInGoogleSheets(List<TodoItem> todoList)
        {
            try
            {
                var currentData = await _sheetsApiService.GetValuesAsync(SPREADSHEET_ID, RANGE_NAME);
                var currentRows = currentData.Count;

                var updateData = new List<List<object>>
        {
            new List<object> { "ID", "Title", "Description", "DueDate", "IsCompleted", "Category", "ParentTaskId", "CreatedAt", "UpdatedAt", "IsDeleted", "LastModifiedDate", "DeletedDate" }
        };

                updateData.AddRange(todoList.Select(item => new List<object>
        {
            item.ID,
            item.Title,
            item.Description,
            item.DueDate?.ToString("M/d/yyyy H:mm"),
            item.IsCompleted.ToString(),
            item.Category,
            item.ParentTaskId,
            item.CreatedAt.ToString("M/d/yyyy H:mm"),
            item.UpdatedAt.ToString("M/d/yyyy H:mm"),
            item.IsDeleted.ToString(),
            item.LastModifiedDate.ToString("M/d/yyyy H:mm"),
            item.DeletedDate?.ToString("M/d/yyyy H:mm") ?? ""
        }));

                string updateRange = $"{RANGE_NAME.Split('!')[0]}!A1:L{Math.Max(currentRows, updateData.Count)}";

                await _sheetsApiService.UpdateValuesAsync(SPREADSHEET_ID, updateRange, updateData);

                _logger.LogInformation("Successfully updated todo list in Google Sheets");

                return await GetTodoListFromGoogleSheets();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating todo list in Google Sheets");
                throw;
            }
        }
        public async Task<List<TodoItem>> AddTodoItem(TodoItem newItem)
        {
            try
            {
                var currentItems = await GetTodoListFromGoogleSheets();
                newItem.ID = GenerateNewId(currentItems, string.IsNullOrEmpty(newItem.ParentTaskId));
                newItem.CreatedAt = DateTimeOffset.Now;
                newItem.UpdatedAt = DateTimeOffset.Now;
                newItem.LastModifiedDate = DateTimeOffset.Now;
                currentItems.Add(newItem);

                var updatedList = await UpdateTodoListInGoogleSheets(currentItems);

                _logger.LogInformation($"Successfully added new item: {newItem.Title}");

                return updatedList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception error adding new todo item");
                throw;
            }
        }

        public async Task<List<TodoItem>> DeleteTodoItem(TodoItem itemToDelete)
        {
            try
            {
                _logger.LogInformation($"Attempting to delete todo item: {itemToDelete.Title}");

                var currentItems = await GetTodoListFromGoogleSheets();
                var itemToRemove = currentItems.FirstOrDefault(i => i.ID == itemToDelete.ID);

                if (itemToRemove != null)
                {
                    itemToRemove.IsDeleted = true;
                    itemToRemove.DeletedDate = DateTime.Now;
                    itemToRemove.LastModifiedDate = DateTimeOffset.Now;

                    var updatedList = await UpdateTodoListInGoogleSheets(currentItems);

                    _logger.LogInformation($"Successfully marked item as deleted: {itemToDelete.Title}");

                    return updatedList;
                }
                else
                {
                    _logger.LogWarning($"Item not found for deletion: {itemToDelete.Title}");
                    return currentItems;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting todo item: {itemToDelete.Title}");
                throw;
            }
        }

        private string GenerateNewId(List<TodoItem> currentItems, bool isMainTask)
        {
            string prefix = isMainTask ? "MTS" : "STS";
            int maxId = currentItems
                .Where(item => item.ID?.StartsWith(prefix) == true)
                .Select(item => int.TryParse(item.ID.Substring(3), out int id) ? id : 0)
                .DefaultIfEmpty(0)
                .Max();
            return $"{prefix}{maxId + 1}";
        }

        public async Task<List<TodoItem>> UpdateTodoItem(TodoItem updatedItem)
        {
            try
            {
                _logger.LogInformation($"Attempting to update todo item: {updatedItem.Title}");

                var currentItems = await GetTodoListFromGoogleSheets();
                var itemToUpdate = currentItems.FirstOrDefault(i => i.ID == updatedItem.ID);

                if (itemToUpdate != null)
                {
                    itemToUpdate.Title = updatedItem.Title;
                    itemToUpdate.Description = updatedItem.Description;
                    itemToUpdate.DueDate = updatedItem.DueDate;
                    itemToUpdate.IsCompleted = updatedItem.IsCompleted;
                    itemToUpdate.Category = updatedItem.Category;
                    itemToUpdate.ParentTaskId = updatedItem.ParentTaskId;
                    itemToUpdate.UpdatedAt = DateTimeOffset.Now;
                    itemToUpdate.LastModifiedDate = DateTimeOffset.Now;

                    var updatedList = await UpdateTodoListInGoogleSheets(currentItems);

                    _logger.LogInformation($"Successfully updated item: {updatedItem.Title}");

                    return updatedList;
                }
                else
                {
                    _logger.LogWarning($"Item not found for update: {updatedItem.Title}");
                    return currentItems;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating todo item: {updatedItem.Title}");
                throw;
            }
        }

        public async Task<List<TodoItem>> GetSubtasks(string parentTaskId)
        {
            try
            {
                _logger.LogInformation($"Fetching subtasks for parent task ID: {parentTaskId}");

                var allItems = await GetTodoListFromGoogleSheets();
                var subtasks = allItems.Where(item => item.ParentTaskId == parentTaskId).ToList();

                _logger.LogInformation($"Found {subtasks.Count} subtasks for parent task ID: {parentTaskId}");

                return subtasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching subtasks for parent task ID: {parentTaskId}");
                throw;
            }
        }

        public async Task<List<TodoItem>> ToggleTaskCompletion(string taskId)
        {
            try
            {
                _logger.LogInformation($"Toggling completion status for task ID: {taskId}");

                var currentItems = await GetTodoListFromGoogleSheets();
                var taskToToggle = currentItems.FirstOrDefault(i => i.ID == taskId);

                if (taskToToggle != null)
                {
                    taskToToggle.IsCompleted = !taskToToggle.IsCompleted;
                    taskToToggle.UpdatedAt = DateTimeOffset.Now;
                    taskToToggle.LastModifiedDate = DateTimeOffset.Now;

                    var updatedList = await UpdateTodoListInGoogleSheets(currentItems);

                    _logger.LogInformation($"Successfully toggled completion status for task ID: {taskId}. New status: {taskToToggle.IsCompleted}");

                    return updatedList;
                }
                else
                {
                    _logger.LogWarning($"Task not found for toggling completion: {taskId}");
                    return currentItems;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling completion status for task ID: {taskId}");
                throw;
            }
        }

        public async Task<List<TodoItem>> GetTasksByCategory(string category)
        {
            try
            {
                _logger.LogInformation($"Fetching tasks for category: {category}");

                var allItems = await GetTodoListFromGoogleSheets();
                var categoryTasks = allItems.Where(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();

                _logger.LogInformation($"Found {categoryTasks.Count} tasks in category: {category}");

                return categoryTasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching tasks for category: {category}");
                throw;
            }
        }

        public async Task<List<string>> GetAllCategories()
        {
            try
            {
                _logger.LogInformation("Fetching all unique categories");

                var allItems = await GetTodoListFromGoogleSheets();
                var categories = allItems.Select(item => item.Category)
                                         .Distinct()
                                         .Where(c => !string.IsNullOrWhiteSpace(c))
                                         .OrderBy(c => c)
                                         .ToList();

                _logger.LogInformation($"Found {categories.Count} unique categories");

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all categories");
                throw;
            }
        }
    }
}