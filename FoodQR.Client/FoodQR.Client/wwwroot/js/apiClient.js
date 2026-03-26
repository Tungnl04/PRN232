const apiClient = (function() {
    const API_BASE_URL = "https://localhost:7197/api"; // Centralized API port

    // Private helper for fetch with auth
    async function authorizedFetch(endpoint, options = {}) {
        const token = localStorage.getItem('auth_token');
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };

        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        const config = {
            ...options,
            headers: headers
        };

        try {
            const response = await fetch(`${API_BASE_URL}${endpoint}`, config);

            if (response.status === 401) {
                // Token expired or invalid
                console.warn("Session expired. Redirecting to login.");
                localStorage.clear();
                window.location.href = '/Login';
                return null;
            }

            return response;
        } catch (error) {
            console.error("API Fetch Error:", error);
            throw error;
        }
    }

    return {
        // AUTH
        async login(username, password) {
            const res = await fetch(`${API_BASE_URL}/Auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password })
            });
            return res;
        },

        logout() {
            localStorage.clear();
            window.location.href = '/Login';
        },

        // TABLES
        async getTables() {
            const res = await authorizedFetch('/Tables');
            return res ? res.json() : [];
        },

        async updateTableStatus(id, status) {
            return await authorizedFetch(`/Tables/${id}/status`, {
                method: 'PATCH',
                body: JSON.stringify(status)
            });
        },

        async createTable(table) {
            return await authorizedFetch('/Tables', {
                method: 'POST',
                body: JSON.stringify(table)
            });
        },

        async updateTable(id, table) {
            return await authorizedFetch(`/Tables/${id}`, {
                method: 'PUT',
                body: JSON.stringify(table)
            });
        },

        async deleteTable(id) {
            return await authorizedFetch(`/Tables/${id}`, {
                method: 'DELETE'
            });
        },

        // QR Code
        async generateQr(tableId) {
            return await authorizedFetch(`/Tables/${tableId}/generate-qr`, {
                method: 'POST'
            });
        },

        // ORDERS
        async getActiveOrder(tableId) {
            const res = await authorizedFetch(`/Orders/active/${tableId}`);
            if (res && res.ok) return res.json();
            return null;
        },

        // KITCHEN
        async getKitchenItems() {
            const res = await authorizedFetch('/Kitchen/items');
            return res ? res.json() : [];
        },

        async updateItemStatus(itemId, status) {
            return await authorizedFetch(`/Kitchen/items/${itemId}/status`, {
                method: 'PATCH',
                body: JSON.stringify(status)
            });
        },

        // CATEGORIES
        async getCategories() {
            const res = await fetch(`${API_BASE_URL}/Categories`);
            return res.ok ? res.json() : [];
        },

        async createCategory(category) {
            return await authorizedFetch('/Categories', {
                method: 'POST',
                body: JSON.stringify(category)
            });
        },

        async updateCategory(id, category) {
            return await authorizedFetch(`/Categories/${id}`, {
                method: 'PUT',
                body: JSON.stringify(category)
            });
        },

        async deleteCategory(id) {
            return await authorizedFetch(`/Categories/${id}`, {
                method: 'DELETE'
            });
        },

        // PRODUCTS
        async getProducts() {
            const res = await fetch(`${API_BASE_URL}/Products`);
            return res.ok ? res.json() : [];
        },

        async createProduct(product) {
            return await authorizedFetch('/Products', {
                method: 'POST',
                body: JSON.stringify(product)
            });
        },

        async updateProduct(id, product) {
            return await authorizedFetch(`/Products/${id}`, {
                method: 'PUT',
                body: JSON.stringify(product)
            });
        },

        async deleteProduct(id) {
            return await authorizedFetch(`/Products/${id}`, {
                method: 'DELETE'
            });
        },

        // USERS
        async getUsers() {
            const res = await authorizedFetch('/Users');
            return res ? res.json() : [];
        },

        async createUser(user) {
            return await authorizedFetch('/Users', {
                method: 'POST',
                body: JSON.stringify(user)
            });
        },

        async updateUser(id, user) {
            return await authorizedFetch(`/Users/${id}`, {
                method: 'PUT',
                body: JSON.stringify(user)
            });
        },

        async deleteUser(id) {
            return await authorizedFetch(`/Users/${id}`, {
                method: 'DELETE'
            });
        },

        // STATS
        async getDashboardStats() {
            // Placeholder: This can be a new endpoint or calculated from current data
            const res = await authorizedFetch('/Orders/stats/overview');
            return res ? res.json() : { totalOrders: 0, totalRevenue: 0, activeTables: 0 };
        },

        // UTILS
        isAuthenticated() {
            return !!localStorage.getItem('auth_token');
        },

        getUser() {
            return {
                role: (localStorage.getItem('user_role') || "").toLowerCase(),
                name: localStorage.getItem('user_name') || ""
            };
        }
    };
})();
