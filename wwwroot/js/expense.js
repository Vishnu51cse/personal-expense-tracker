/**
 * expense.js - Handles Expense operations with JWT-protected API endpoints
 */

const ExpenseApp = {
    currentCategory: "All",
    categories: [],

    // Initialize Dashboard
    async initDashboard() {
        if (!Auth.requireAuth()) return;

        await this.loadCategories("categoryFilterSelect");
        await this.loadSummary();
        await this.loadExpenses();

        // Listen for filter changes
        const filterSelect = document.getElementById("categoryFilterSelect");
        if (filterSelect) {
            filterSelect.addEventListener("change", (e) => {
                this.currentCategory = e.target.value;
                this.loadExpenses();
            });
        }
    },

    // Load predefined categories from API
    async loadCategories(selectId, selectedValue = "") {
        const select = document.getElementById(selectId);
        if (!select) return;

        try {
            const response = await fetch("/api/expenses/categories");
            if (!response.ok) return;

            this.categories = await response.json();

            // Check if this is a filter select (with "All" option) or form select
            const isFilter = selectId.includes("Filter");
            select.innerHTML = isFilter ? '<option value="All">All Categories</option>' : '<option value="">-- Select Category --</option>';

            this.categories.forEach(cat => {
                const opt = document.createElement("option");
                opt.value = cat;
                opt.textContent = cat;
                if (cat === selectedValue) {
                    opt.selected = true;
                }
                select.appendChild(opt);
            });
        } catch (err) {
            console.error("Failed to load categories:", err);
        }
    },

    // Load Expense Summary (Total, Monthly Total, Count, Breakdown)
    async loadSummary() {
        try {
            const response = await Auth.fetchWithAuth("/api/expenses/summary");
            if (!response.ok) return;

            const summary = await response.json();

            // Update Total Expenses card
            const totalEl = document.getElementById("summaryTotalAmount");
            if (totalEl) totalEl.textContent = this.formatCurrency(summary.totalAmount);

            // Update This Month card
            const monthEl = document.getElementById("summaryThisMonthAmount");
            if (monthEl) monthEl.textContent = this.formatCurrency(summary.thisMonthTotal);

            // Update Total Count card
            const countEl = document.getElementById("summaryTotalCount");
            if (countEl) countEl.textContent = summary.totalCount;

            // Render category breakdown progress
            this.renderCategoryBreakdown(summary.categoryBreakdown);

        } catch (err) {
            console.error("Failed to load summary:", err);
        }
    },

    // Render Category Breakdown list
    renderCategoryBreakdown(breakdown) {
        const container = document.getElementById("categoryBreakdownContainer");
        if (!container) return;

        if (!breakdown || breakdown.length === 0) {
            container.innerHTML = '<p class="text-muted small mb-0">No expenses recorded yet.</p>';
            return;
        }

        let html = '<div class="list-group list-group-flush">';
        breakdown.forEach(item => {
            const percentage = item.percentage.toFixed(1);
            html += `
                <div class="list-group-item px-0 py-2 border-0">
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <span class="small fw-semibold">${escapeHtml(item.category)}</span>
                        <span class="small text-muted">${this.formatCurrency(item.totalAmount)} (${percentage}%)</span>
                    </div>
                    <div class="progress" style="height: 6px;">
                        <div class="progress-bar bg-primary" role="progressbar" style="width: ${percentage}%"></div>
                    </div>
                </div>
            `;
        });
        html += '</div>';
        container.innerHTML = html;
    },

    // Load expense records based on current category filter
    async loadExpenses() {
        const tableBody = document.getElementById("expensesTableBody");
        const emptyState = document.getElementById("expensesEmptyState");
        const loadingState = document.getElementById("expensesLoadingState");
        const tableWrapper = document.getElementById("expensesTableWrapper");

        if (loadingState) loadingState.classList.remove("d-none");
        if (emptyState) emptyState.classList.add("d-none");
        if (tableWrapper) tableWrapper.classList.add("d-none");

        try {
            const url = this.currentCategory && this.currentCategory !== "All"
                ? `/api/expenses?category=${encodeURIComponent(this.currentCategory)}`
                : "/api/expenses";

            const response = await Auth.fetchWithAuth(url);
            if (!response.ok) throw new Error("Failed to load expenses");

            const expenses = await response.json();

            if (loadingState) loadingState.classList.add("d-none");

            if (!expenses || expenses.length === 0) {
                if (emptyState) emptyState.classList.remove("d-none");
                return;
            }

            if (tableWrapper) tableWrapper.classList.remove("d-none");

            if (tableBody) {
                tableBody.innerHTML = expenses.map(exp => `
                    <tr>
                        <td class="align-middle">
                            <span class="text-muted small">${this.formatDate(exp.date)}</span>
                        </td>
                        <td class="align-middle">
                            <div class="fw-semibold text-dark">${escapeHtml(exp.title)}</div>
                            ${exp.description ? `<div class="text-muted small">${escapeHtml(exp.description)}</div>` : ''}
                        </td>
                        <td class="align-middle">
                            <span class="badge bg-light text-dark border badge-category">
                                ${escapeHtml(exp.category)}
                            </span>
                        </td>
                        <td class="align-middle text-end fw-bold text-dark fs-6">
                            ${this.formatCurrency(exp.amount)}
                        </td>
                        <td class="align-middle text-end">
                            <div class="btn-group btn-group-sm" role="group">
                                <a href="/Expense/Edit/${exp.id}" class="btn btn-outline-secondary" title="Edit Expense">
                                    <i class="bi bi-pencil"></i>
                                </a>
                                <button type="button" class="btn btn-outline-danger" onclick="ExpenseApp.deleteExpense(${exp.id}, '${escapeHtml(exp.title)}')" title="Delete Expense">
                                    <i class="bi bi-trash"></i>
                                </button>
                            </div>
                        </td>
                    </tr>
                `).join("");
            }

        } catch (err) {
            console.error("Error loading expenses:", err);
            if (loadingState) loadingState.classList.add("d-none");
            if (emptyState) {
                emptyState.textContent = "Error loading expenses: " + err.message;
                emptyState.classList.remove("d-none");
            }
        }
    },

    // Delete an expense
    async deleteExpense(id, title) {
        if (!confirm(`Are you sure you want to delete "${title}"?`)) {
            return;
        }

        try {
            const response = await Auth.fetchWithAuth(`/api/expenses/${id}`, {
                method: "DELETE"
            });

            if (!response.ok) {
                const data = await response.json().catch(() => ({}));
                throw new Error(data.message || "Failed to delete expense");
            }

            showGlobalAlert(`Expense "${title}" was successfully deleted.`, "success");
            await this.loadSummary();
            await this.loadExpenses();

        } catch (err) {
            showGlobalAlert(err.message, "danger");
        }
    },

    // Format decimal number as INR currency (Rupees: ₹)
    formatCurrency(val) {
        return new Intl.NumberFormat("en-IN", {
            style: "currency",
            currency: "INR",
            maximumFractionDigits: 2
        }).format(val || 0);
    },

    // Format ISO date string into readable Date
    formatDate(dateString) {
        if (!dateString) return "";
        const d = new Date(dateString);
        return d.toLocaleDateString("en-US", {
            year: "numeric",
            month: "short",
            day: "numeric"
        });
    }
};
