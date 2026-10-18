/**
 * auth.js - JWT Authentication & Token Management
 * Handles storing, retrieving, and attaching JWT bearer tokens securely.
 */

const TOKEN_KEY = "expense_tracker_jwt";
const USER_KEY = "expense_tracker_user";

const Auth = {
    // Save token and user details to sessionStorage
    // (sessionStorage is cleared when tab is closed, avoiding token persistence issues on shared machines)
    saveAuth(token, user) {
        if (!token) return;
        sessionStorage.setItem(TOKEN_KEY, token);
        if (user) {
            sessionStorage.setItem(USER_KEY, JSON.stringify(user));
        }
        this.updateNavbar();
    },

    // Retrieve stored JWT token
    getToken() {
        return sessionStorage.getItem(TOKEN_KEY);
    },

    // Retrieve stored user information
    getUser() {
        const userJson = sessionStorage.getItem(USER_KEY);
        try {
            return userJson ? JSON.parse(userJson) : null;
        } catch {
            return null;
        }
    },

    // Check whether user is currently authenticated with a non-expired token
    isAuthenticated() {
        const token = this.getToken();
        if (!token) return false;

        try {
            // Decode payload of JWT (base64url)
            const parts = token.split('.');
            if (parts.length !== 3) return false;

            const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
            const exp = payload.exp;
            if (!exp) return true;

            // exp is in seconds, Date.now() in ms
            const isExpired = Date.now() >= exp * 1000;
            if (isExpired) {
                this.logout(false);
                return false;
            }
            return true;
        } catch {
            return false;
        }
    },

    // Clear session and redirect to login
    logout(redirect = true) {
        sessionStorage.removeItem(TOKEN_KEY);
        sessionStorage.removeItem(USER_KEY);
        this.updateNavbar();
        if (redirect) {
            window.location.href = "/Account/Login";
        }
    },

    // Redirect to login if user is not authenticated
    requireAuth() {
        if (!this.isAuthenticated()) {
            window.location.href = "/Account/Login?returnUrl=" + encodeURIComponent(window.location.pathname);
            return false;
        }
        return true;
    },

    // Redirect to dashboard if user is already logged in
    redirectIfAuthenticated() {
        if (this.isAuthenticated()) {
            window.location.href = "/Expense/Index";
            return true;
        }
        return false;
    },

    // Helper to get default headers with Bearer token
    getHeaders(extraHeaders = {}) {
        const headers = {
            "Content-Type": "application/json",
            ...extraHeaders
        };
        const token = this.getToken();
        if (token) {
            headers["Authorization"] = `Bearer ${token}`;
        }
        return headers;
    },

    // Wrapper around fetch that adds Bearer token and handles 401 Unauthorized
    async fetchWithAuth(url, options = {}) {
        const token = this.getToken();
        const headers = {
            "Content-Type": "application/json",
            ...(options.headers || {})
        };

        if (token) {
            headers["Authorization"] = `Bearer ${token}`;
        }

        const response = await fetch(url, {
            ...options,
            headers
        });

        if (response.status === 401) {
            // Token expired or invalid
            this.logout(true);
            throw new Error("Session expired. Please log in again.");
        }

        return response;
    },

    // Updates navbar elements based on auth state
    updateNavbar() {
        const authNav = document.getElementById("navbarAuthSection");
        if (!authNav) return;

        const isAuth = this.isAuthenticated();
        const user = this.getUser();

        if (isAuth && user) {
            authNav.innerHTML = `
                <li class="nav-item dropdown">
                    <a class="nav-link dropdown-toggle text-dark fw-bold d-flex align-items-center" href="#" role="button" data-bs-toggle="dropdown">
                        <span class="badge bg-primary rounded-pill me-1"><i class="bi bi-person-fill"></i></span>
                        ${escapeHtml(user.username || 'User')}
                    </a>
                    <ul class="dropdown-menu dropdown-menu-end shadow-sm">
                        <li><span class="dropdown-item-text text-muted small">${escapeHtml(user.email || '')}</span></li>
                        <li><hr class="dropdown-divider"></li>
                        <li><a class="dropdown-item" href="/Expense/Index"><i class="bi bi-speedometer2 me-2"></i>Dashboard</a></li>
                        <li><a class="dropdown-item" href="/Expense/Create"><i class="bi bi-plus-circle me-2"></i>Add Expense</a></li>
                        <li><hr class="dropdown-divider"></li>
                        <li>
                            <button class="dropdown-item text-danger" onclick="Auth.logout(true)">
                                <i class="bi bi-box-arrow-right me-2"></i>Logout
                            </button>
                        </li>
                    </ul>
                </li>
            `;
        } else {
            authNav.innerHTML = `
                <li class="nav-item">
                    <a class="nav-link text-dark" href="/Account/Login">
                        <i class="bi bi-box-arrow-in-right me-1"></i>Login
                    </a>
                </li>
                <li class="nav-item">
                    <a class="btn btn-primary btn-sm ms-2 text-white" href="/Account/Register">
                        <i class="bi bi-person-plus me-1"></i>Register
                    </a>
                </li>
            `;
        }
    }
};

// Utility function to escape HTML
function escapeHtml(text) {
    if (!text) return "";
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
}

// Auto-update navbar when DOM is ready
document.addEventListener("DOMContentLoaded", () => {
    Auth.updateNavbar();
});
