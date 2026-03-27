const apiClient = (function() {
    // Auto-detect: nếu chạy trên Azure thì dùng Azure API, nếu localhost thì dùng localhost
    const API_BASE_URL = window.location.hostname.includes('localhost')
        ? "https://localhost:7197/api"
        : "https://foodqrrestaurant-cbdwbzfcfxecdfay.southeastasia-01.azurewebsites.net/api";

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
        async changePassword(oldPassword, newPassword) {
            return await authorizedFetch('/Auth/change-password', {
                method: 'POST',
                body: JSON.stringify({ oldPassword, newPassword })
            });
        },
        async resetPassword(userId) {
            return await authorizedFetch(`/Auth/reset-password/${userId}`, {
                method: 'POST'
            });
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

        async getTable(id) {
            const res = await authorizedFetch(`/Tables/${id}`);
            return res && res.ok ? res.json() : null;
        },
        // ORDERS
        async getActiveOrder(tableId, token) {
            const tokenParam = token ? `?token=${encodeURIComponent(token)}` : '';
            const res = await authorizedFetch(`/Orders/active/${tableId}${tokenParam}`);
            if (res && res.ok) return res.json();
            return null;
        },

        // PAYMENTS
        async checkoutCash(orderId) {
            return await authorizedFetch(`/Payments/${orderId}/process?method=cash&simulateSuccess=true`, {
                method: 'POST'
            });
        },

        async createVnPayUrl(orderId) {
            return await authorizedFetch(`/Payments/${orderId}/vnpay-url`, {
                method: 'POST'
            });
        },

        // STORE CONFIG
        async getStoreConfig() {
            const res = await fetch(`${API_BASE_URL}/StoreConfig`);
            return res.ok ? res.json() : null;
        },

        async updateStoreConfig(data) {
            return await authorizedFetch('/StoreConfig', {
                method: 'PUT',
                body: JSON.stringify(data)
            });
        },

        async getOrders(limit = 10) {
            const res = await authorizedFetch(`/Orders?limit=${limit}`);
            return res ? res.json() : [];
        },

        async mergeOrder(targetId, sourceId) {
            return await authorizedFetch(`/Orders/${targetId}/merge-from/${sourceId}`, {
                method: 'POST'
            });
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

        // COMBOS
        async getCombos(includeHidden = false) {
            const res = await fetch(`${API_BASE_URL}/Combos?includeHidden=${includeHidden}`);
            return res.ok ? res.json() : [];
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
            const res = await authorizedFetch('/Dashboard/stats');
            return res ? res.json() : {};
        },

        // UPLOAD
        async uploadImage(file) {
            const formData = new FormData();
            formData.append('file', file);
            
            const token = localStorage.getItem('auth_token');
            const headers = {};
            if (token) headers['Authorization'] = `Bearer ${token}`;
            
            try {
                const res = await fetch(`${API_BASE_URL}/Uploads`, {
                    method: 'POST',
                    headers: headers,
                    body: formData
                });
                if (res.ok) {
                    const data = await res.json();
                    // Append true frontend URL instead of just relative if working locally or Azure
                    return data.url; 
                }
                return null;
            } catch (e) {
                console.error('Upload failed', e);
                return null;
            }
        },

        // COUPONS
        async getCoupons() {
            const res = await authorizedFetch('/Coupons');
            return res ? res.json() : [];
        },

        async createCoupon(coupon) {
            return await authorizedFetch('/Coupons', {
                method: 'POST',
                body: JSON.stringify(coupon)
            });
        },

        async updateCoupon(id, coupon) {
            return await authorizedFetch(`/Coupons/${id}`, {
                method: 'PUT',
                body: JSON.stringify(coupon)
            });
        },

        async deleteCoupon(id) {
            return await authorizedFetch(`/Coupons/${id}`, {
                method: 'DELETE'
            });
        },

        async validateCoupon(code, orderTotal) {
            const res = await fetch(`${API_BASE_URL}/Coupons/validate`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ code, orderTotal })
            });
            if (res.ok) return res.json();
            throw await res.json();
        },

        // UTILS
        isAuthenticated() {
            return !!localStorage.getItem('auth_token');
        },

        getUser() {
            return {
                id: localStorage.getItem('user_id') || "0",
                role: (localStorage.getItem('user_role') || "").toLowerCase(),
                name: localStorage.getItem('user_name') || "",
                mustChangePassword: localStorage.getItem('must_change_password') === 'true'
            };
        }
    };
})();
