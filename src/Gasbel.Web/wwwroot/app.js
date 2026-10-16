// Gasbel B2B Frontend Logic - Catalog & Admin ABM
let categories = [];
let customerGroups = [];
let customers = [];
let products = [];
let adminProducts = [];
let currentView = 'catalog'; // 'catalog' | 'admin'
let selectedCategoryId = null;
let selectedGroupId = "";
let selectedCustomerId = "";
let activeAdminFilter = 'all';
let currentAdminPage = 1;
const ADMIN_PAGE_SIZE = 50;

let quoteItems = []; // { product, quantity, packagingSelected, effectiveUnits, unitPrice, wholesalePrice, lineSubtotal, lineWholesaleSubtotal }
let productSelections = {}; // productId -> { quantity, packaging }

// Reglas Comerciales y Clientes Especiales (reunión con cliente)
let commercialSettings = {
  minPurchaseForDiscount: 50000,
  minPurchaseForFreeShipping: 100000,
  whatsAppProduction: "2616799084",
  whatsAppTesting: "2613890532",
  useTestingNumber: true,
  freeShippingLegendPrefix: "El envío es gratis a partir de "
};
let specialClients = [];
let currentSpecialClient = null;
let activeAdminTab = 'products'; // 'products' | 'clients' | 'settings'

const API_BASE = '/api';

// Format currency
function formatCurrency(amount) {
  if (amount === undefined || amount === null || isNaN(amount)) return '$ 0,00';
  return new Intl.NumberFormat('es-AR', {
    style: 'currency',
    currency: 'ARS',
    minimumFractionDigits: 2
  }).format(amount);
}

// Toast Notification
function showToast(message, isSuccess = true) {
  const toast = document.getElementById('toastNotification');
  const msgEl = document.getElementById('toastMessage');
  const icon = document.getElementById('toastIcon');

  msgEl.textContent = message;
  icon.setAttribute('data-lucide', isSuccess ? 'check-circle' : 'alert-circle');
  icon.className = isSuccess ? 'w-4 h-4 text-emerald-400' : 'w-4 h-4 text-rose-400';
  lucide.createIcons();

  toast.classList.remove('translate-y-20', 'opacity-0');
  toast.classList.add('translate-y-0', 'opacity-100');

  setTimeout(() => {
    toast.classList.remove('translate-y-0', 'opacity-100');
    toast.classList.add('translate-y-20', 'opacity-0');
  }, 3500);
}

// ==========================================
// 0. AUTENTICACIÓN Y CONTROL DE ACCESO ADMIN
// ==========================================

let isAdminAuthenticated = false;

function initAdminAuth() {
  const stored = localStorage.getItem('gasbel_admin_auth');
  if (stored === 'active_session_gasbel') {
    isAdminAuthenticated = true;
  }

  // Comprobar parámetros en la URL (ej: ?admin o ?admin=1 o #admin)
  const urlParams = new URLSearchParams(window.location.search);
  const hasAdminParam = urlParams.has('admin') || window.location.hash === '#admin';

  updateAdminUI();

  if (hasAdminParam) {
    if (!isAdminAuthenticated) {
      setTimeout(() => openAdminAuthModal(), 300);
    } else {
      switchView('admin');
    }
  }
}

function updateAdminUI() {
  const adminNavCenter = document.getElementById('adminNavCenter');
  const btnApiDocs = document.getElementById('btnApiDocs');
  const simWrapper = document.getElementById('simulatorSelectorWrapper');
  const btnAdminAccess = document.getElementById('btnAdminAccess');
  const adminBadgeWrapper = document.getElementById('adminBadgeWrapper');
  const profileBannerText = document.getElementById('profileBannerText');

  if (isAdminAuthenticated) {
    if (adminNavCenter) {
      adminNavCenter.classList.remove('hidden');
      adminNavCenter.classList.add('flex');
    }
    if (btnApiDocs) {
      btnApiDocs.classList.remove('hidden');
      btnApiDocs.classList.add('inline-flex');
    }
    if (btnAdminAccess) btnAdminAccess.classList.add('hidden');
    if (adminBadgeWrapper) {
      adminBadgeWrapper.classList.remove('hidden');
      adminBadgeWrapper.classList.add('flex');
    }
    if (profileBannerText && !selectedGroupId) {
      profileBannerText.textContent = "Modo Administrador activo. Puedes gestionar artículos en ABM o simular perfiles comerciales.";
    }
  } else {
    if (adminNavCenter) {
      adminNavCenter.classList.add('hidden');
      adminNavCenter.classList.remove('flex');
    }
    if (btnApiDocs) {
      btnApiDocs.classList.add('hidden');
      btnApiDocs.classList.remove('inline-flex');
    }
    if (btnAdminAccess) btnAdminAccess.classList.remove('hidden');
    if (adminBadgeWrapper) {
      adminBadgeWrapper.classList.add('hidden');
      adminBadgeWrapper.classList.remove('flex');
    }
    if (profileBannerText && !selectedGroupId) {
      profileBannerText.textContent = "Catálogo oficial de artículos Gasbel. Precios actualizados en tiempo real.";
    }
    if (currentView === 'admin') {
      switchView('catalog');
    }
  }

  // El selector genérico de descuento se mantiene oculto según requerimiento del cliente
  if (simWrapper) {
    simWrapper.classList.add('hidden');
    simWrapper.classList.remove('flex');
  }
}

function openAdminAuthModal() {
  const modal = document.getElementById('adminAuthModal');
  const input = document.getElementById('adminPinInput');
  const error = document.getElementById('adminAuthError');
  if (error) error.classList.add('hidden');
  if (input) {
    input.value = '';
    input.classList.remove('border-rose-400', 'bg-rose-50');
  }
  if (modal) modal.classList.remove('hidden');
  setTimeout(() => { if (input) input.focus(); }, 100);
  lucide.createIcons();
}

function closeAdminAuthModal() {
  const modal = document.getElementById('adminAuthModal');
  if (modal) modal.classList.add('hidden');
}

function togglePinVisibility() {
  const input = document.getElementById('adminPinInput');
  const icon = document.getElementById('pinVisibilityIcon');
  if (!input || !icon) return;
  if (input.type === 'password') {
    input.type = 'text';
    icon.setAttribute('data-lucide', 'eye-off');
  } else {
    input.type = 'password';
    icon.setAttribute('data-lucide', 'eye');
  }
  lucide.createIcons();
}

function submitAdminAuth() {
  const input = document.getElementById('adminPinInput');
  const error = document.getElementById('adminAuthError');
  const pin = (input ? input.value : '').trim().toLowerCase();

  // Claves de acceso válidas para el administrador (fáciles de recordar)
  const validKeys = ['gasbel', 'gasbel2026', 'admin', '1234'];
  if (validKeys.includes(pin)) {
    isAdminAuthenticated = true;
    localStorage.setItem('gasbel_admin_auth', 'active_session_gasbel');
    closeAdminAuthModal();
    updateAdminUI();
    showToast('Acceso de Administrador concedido');
    switchView('admin');
  } else {
    if (error) error.classList.remove('hidden');
    if (input) {
      input.classList.add('border-rose-400', 'bg-rose-50');
      setTimeout(() => input.classList.remove('border-rose-400', 'bg-rose-50'), 1500);
    }
  }
}

function logoutAdmin() {
  isAdminAuthenticated = false;
  localStorage.removeItem('gasbel_admin_auth');
  updateAdminUI();
  switchView('catalog');
  showToast('Sesión de administración cerrada');
}

// Init App
document.addEventListener('DOMContentLoaded', async () => {
  initAdminAuth();
  await Promise.all([
    loadCommercialSettings(),
    loadSpecialClients(),
    loadCustomerGroups(),
    loadCustomers(),
    loadCategories()
  ]);
  detectClientFromUrl();
  await Promise.all([
    loadProducts(),
    loadAdminStats()
  ]);
  lucide.createIcons();
});

function detectClientFromUrl() {
  const urlParams = new URLSearchParams(window.location.search);
  const clientParam = urlParams.get('cliente') || urlParams.get('c') || urlParams.get('client');
  if (!clientParam) return;

  const trimmed = clientParam.trim().toLowerCase();
  const match = specialClients.find(c => 
    c.isActive && (c.id.toLowerCase() === trimmed || c.name.toLowerCase() === trimmed)
  );

  if (match) {
    currentSpecialClient = match;
    showSpecialClientBanner(match);

    // Pre-cargar campos del cotizador
    const nameInput = document.getElementById('quoteCustomerName');
    const addrInput = document.getElementById('quoteCustomerAddress');
    const phoneInput = document.getElementById('quoteCustomerPhone');
    if (nameInput) nameInput.value = match.name;
    if (addrInput && match.address) addrInput.value = match.address;
    if (phoneInput && match.phone) phoneInput.value = match.phone;

    // Mostrar badge en el drawer
    const badge = document.getElementById('specialClientBadge');
    const badgeText = document.getElementById('specialClientBadgeText');
    if (badge && badgeText) {
      badge.classList.remove('hidden');
      badgeText.textContent = `🌟 Cliente Especial identificado: ${match.name} — ¡${match.discountPercentage}% OFF asignado!`;
    }

    updateDrawerTotals();
  }
}

function showSpecialClientBanner(client) {
  const banner = document.getElementById('specialClientWelcomeBanner');
  const nameEl = document.getElementById('welcomeClientName');
  const badgeEl = document.getElementById('welcomeClientDiscountBadge');
  const subEl = document.getElementById('welcomeClientSubtitle');

  if (banner && nameEl && badgeEl) {
    nameEl.textContent = `¡Hola, ${client.name}!`;
    badgeEl.textContent = `${client.discountPercentage}% OFF EXCLUSIVO`;
    if (subEl) {
      subEl.textContent = `Tenés asignado un beneficio del ${client.discountPercentage}% de descuento en tus pedidos mayoristas. Los precios de los artículos ya reflejan tu condición especial.`;
    }
    banner.classList.remove('hidden');
    lucide.createIcons();
  }
}

// View Switcher
function switchView(viewName) {
  if (viewName === 'admin' && !isAdminAuthenticated) {
    openAdminAuthModal();
    return;
  }

  currentView = viewName;
  const viewCatalog = document.getElementById('viewCatalog');
  const viewAdmin = document.getElementById('viewAdmin');
  const navBtnCatalog = document.getElementById('navBtnCatalog');
  const navBtnAdmin = document.getElementById('navBtnAdmin');
  const profileBanner = document.getElementById('profileBanner');
  const simWrapper = document.getElementById('simulatorSelectorWrapper');

  if (viewName === 'admin') {
    viewCatalog.classList.add('hidden');
    viewAdmin.classList.remove('hidden');
    profileBanner.classList.add('hidden');
    if (simWrapper) {
      simWrapper.classList.add('hidden');
      simWrapper.classList.remove('flex');
    }

    navBtnAdmin.className = "flex items-center gap-2 px-3.5 py-1.5 text-xs font-bold rounded-lg transition-all bg-white text-slate-900 shadow-sm";
    navBtnCatalog.className = "flex items-center gap-2 px-3.5 py-1.5 text-xs font-bold rounded-lg transition-all text-slate-600 hover:text-slate-900";

    switchAdminTab(activeAdminTab);
    loadAdminStats();
    loadAdminProducts();
  } else {
    viewCatalog.classList.remove('hidden');
    viewAdmin.classList.add('hidden');
    profileBanner.classList.remove('hidden');
    if (simWrapper) {
      simWrapper.classList.add('hidden');
      simWrapper.classList.remove('flex');
    }

    navBtnCatalog.className = "flex items-center gap-2 px-3.5 py-1.5 text-xs font-bold rounded-lg transition-all bg-white text-slate-900 shadow-sm";
    navBtnAdmin.className = "flex items-center gap-2 px-3.5 py-1.5 text-xs font-bold rounded-lg transition-all text-slate-600 hover:text-slate-900";

    loadProducts();
  }

  lucide.createIcons();
}

// ==========================================
// 1. CARGA DE DATOS B2B Y CLIENTES
// ==========================================

async function loadCustomerGroups() {
  try {
    const res = await fetch(`${API_BASE}/customer-groups`);
    if (res.ok) {
      customerGroups = await res.json();
      const select = document.getElementById('groupSelector');
      select.innerHTML = '<option value="">P. Venta 1 (Minorista / Base)</option>';
      customerGroups.forEach(g => {
        const opt = document.createElement('option');
        opt.value = g.id;
        opt.textContent = `${g.name} (${g.defaultDiscountPercentage}% off)`;
        select.appendChild(opt);
      });

      checkUrlProfileParam();
    }
  } catch (err) {
    console.error('Error al cargar grupos:', err);
  }
}

function checkUrlProfileParam() {
  const urlParams = new URLSearchParams(window.location.search);
  const perfilParam = (urlParams.get('perfil') || urlParams.get('grupo') || urlParams.get('lista') || '').toLowerCase().trim();
  if (perfilParam && customerGroups.length > 0) {
    const match = customerGroups.find(g => 
      g.id.toLowerCase() === perfilParam || 
      g.name.toLowerCase().includes(perfilParam) ||
      (g.code && g.code.toLowerCase().includes(perfilParam))
    );
    if (match) {
      const select = document.getElementById('groupSelector');
      if (select) {
        select.value = match.id;
        onGroupChanged();
      }
    }
  }
}

async function loadCustomers() {
  try {
    const res = await fetch(`${API_BASE}/customers`);
    if (res.ok) {
      customers = await res.json();
    }
  } catch (err) {
    console.error('Error al cargar clientes:', err);
  }
}

async function loadCategories() {
  try {
    const res = await fetch(`${API_BASE}/categories`);
    if (res.ok) {
      categories = await res.json();
      renderCategorySelect();
      populateCategorySelects();
    }
  } catch (err) {
    console.error('Error al cargar categorías:', err);
  }
}

function renderCategorySelect() {
  const select = document.getElementById('categorySelect');
  if (!select) return;
  const totalCount = categories.reduce((acc, c) => acc + c.productCount, 0);

  select.innerHTML = `<option value="">Todas las Categorías (${totalCount})</option>`;
  categories.forEach(c => {
    const opt = document.createElement('option');
    opt.value = c.id;
    opt.textContent = `${c.name} (${c.productCount})`;
    if (selectedCategoryId === c.id) {
      opt.selected = true;
    }
    select.appendChild(opt);
  });
}

function onCategorySelectChange(catId) {
  catalogCurrentPage = 1;
  selectedCategoryId = catId || null;
  loadProducts();
}

function selectCategory(catId) {
  catalogCurrentPage = 1;
  selectedCategoryId = catId || null;
  const select = document.getElementById('categorySelect');
  if (select) {
    select.value = selectedCategoryId || '';
  }
  loadProducts();
}

function populateCategorySelects() {
  const select = document.getElementById('editCategoryId');
  if (!select) return;
  select.innerHTML = '';
  categories.forEach(c => {
    const opt = document.createElement('option');
    opt.value = c.id;
    opt.textContent = c.name;
    select.appendChild(opt);
  });
}

function onGroupChanged() {
  catalogCurrentPage = 1;
  const select = document.getElementById('groupSelector');
  selectedGroupId = select.value;

  const banner = document.getElementById('profileBannerText');
  if (!selectedGroupId) {
    banner.textContent = "Precios de lista base (P. Venta 1). Cambia el perfil comercial para consultar precios mayoristas (P. Venta 2) o bonificaciones por volumen.";
    selectedCustomerId = "";
  } else {
    const group = customerGroups.find(g => g.id === selectedGroupId);
    const matchingCust = customers.find(c => c.customerGroupId === selectedGroupId);
    selectedCustomerId = matchingCust ? matchingCust.id : "";
    banner.textContent = `Perfil activo: ${group.name} (${group.defaultDiscountPercentage}% off). Aplicando precios preferenciales para ${group.description}`;
  }

  loadProducts();
  recalculateQuoteItems();
}

let searchDebounceTimer = null;
function onSearchInput() {
  catalogCurrentPage = 1;
  const searchInput = document.getElementById('searchInput');
  const clearBtn = document.getElementById('clearSearchBtn');
  if (clearBtn) {
    if (searchInput && searchInput.value.trim().length > 0) {
      clearBtn.classList.remove('hidden');
    } else {
      clearBtn.classList.add('hidden');
    }
  }
  clearTimeout(searchDebounceTimer);
  searchDebounceTimer = setTimeout(() => {
    loadProducts();
  }, 250);
}

function clearSearchInput() {
  catalogCurrentPage = 1;
  const searchInput = document.getElementById('searchInput');
  const clearBtn = document.getElementById('clearSearchBtn');
  if (searchInput) searchInput.value = '';
  if (clearBtn) clearBtn.classList.add('hidden');
  loadProducts();
}

// ==========================================
// 2. VISTA CATÁLOGO B2B (CARDS ENRIQUECIDAS & PAGINACIÓN)
// ==========================================

let catalogCurrentPage = 1;
const catalogPageSize = 8; // 8 productos por página para navegación ágil

async function loadProducts() {
  const grid = document.getElementById('productGrid');
  const emptyState = document.getElementById('emptyState');
  const paginationContainer = document.getElementById('catalogPagination');
  const countText = document.getElementById('catalogCountText');
  const search = document.getElementById('searchInput').value.trim();

  let url = `${API_BASE}/products?`;
  if (selectedCategoryId) url += `categoryId=${selectedCategoryId}&`;
  if (search) url += `search=${encodeURIComponent(search)}&`;
  if (selectedGroupId) url += `customerGroupId=${selectedGroupId}&`;
  if (selectedCustomerId) url += `customerId=${selectedCustomerId}&`;

  try {
    const res = await fetch(url);
    if (res.ok) {
      products = await res.json();
      if (countText) {
        countText.textContent = `${products.length} artículo${products.length === 1 ? '' : 's'}`;
      }
      if (products.length === 0) {
        grid.innerHTML = '';
        emptyState.classList.remove('hidden');
        if (paginationContainer) paginationContainer.classList.add('hidden');
      } else {
        emptyState.classList.add('hidden');
        renderProductGrid();
      }
    } else {
      if (countText) countText.textContent = 'Error al cargar';
      if (paginationContainer) paginationContainer.classList.add('hidden');
    }
  } catch (err) {
    console.error('Error al cargar productos:', err);
    if (countText) countText.textContent = 'Error de conexión';
    if (paginationContainer) paginationContainer.classList.add('hidden');
  }
}

function getCategoryIcon(catName) {
  const name = (catName || '').toLowerCase();
  if (name.includes('bolsa')) return 'shopping-bag';
  if (name.includes('vaso') || name.includes('copa')) return 'cup-soda';
  if (name.includes('perfum')) return 'sparkles';
  if (name.includes('papel') || name.includes('film')) return 'scroll';
  if (name.includes('envase') || name.includes('bandeja')) return 'box';
  return 'package';
}

function renderProductGrid() {
  const grid = document.getElementById('productGrid');
  const paginationContainer = document.getElementById('catalogPagination');
  grid.innerHTML = '';

  const totalProducts = products.length;
  if (totalProducts === 0) {
    if (paginationContainer) paginationContainer.classList.add('hidden');
    return;
  }

  const totalPages = Math.ceil(totalProducts / catalogPageSize);
  if (catalogCurrentPage > totalPages) catalogCurrentPage = totalPages;
  if (catalogCurrentPage < 1) catalogCurrentPage = 1;

  const startIndex = (catalogCurrentPage - 1) * catalogPageSize;
  const endIndex = Math.min(startIndex + catalogPageSize, totalProducts);
  const pageProducts = products.slice(startIndex, endIndex);

  pageProducts.forEach(p => {
    if (!productSelections[p.id]) {
      productSelections[p.id] = {
        quantity: 1,
        packaging: 'Package',
        calculatedPrice: p.currentCalculatedPrice,
        priceSource: p.currentPriceSource,
        savingsPercentage: p.savingsPercentage,
        unitsNeededForNextTier: null,
        nextTierPrice: null
      };
    }

    const sel = productSelections[p.id];
    const isBox = sel.packaging === 'Box';
    const effectiveUnits = isBox ? sel.quantity * p.unitsPerBox : sel.quantity;
    const catIcon = getCategoryIcon(p.categoryName);

    const card = document.createElement('div');
    card.id = `product-card-${p.id}`;
    card.className = "bg-white rounded-2xl border border-slate-200/80 shadow-sm hover:shadow-md transition-all flex flex-col justify-between overflow-hidden group";

    card.innerHTML = `
      <!-- Product Image / Visual Placeholder -->
      <div class="relative h-44 bg-gradient-to-b from-slate-50 to-slate-100 flex items-center justify-center overflow-hidden border-b border-slate-100">
        ${p.imageUrl ? `
          <img src="${p.imageUrl}" alt="${p.name}" class="w-full h-full object-contain p-2 group-hover:scale-105 transition-transform duration-300" />
        ` : `
          <div class="flex flex-col items-center justify-center text-slate-300 group-hover:text-amber-500 transition-colors">
            <i data-lucide="${catIcon}" class="w-14 h-14 stroke-[1.2]"></i>
            <span class="text-[10px] font-semibold text-slate-400 mt-1 uppercase tracking-wider">${p.categoryName}</span>
          </div>
        `}

        <!-- Stock Indicator (Sin número: verde si hay stock, gris si no hay stock) -->
        <span class="absolute top-2.5 right-2.5 inline-flex items-center gap-1.5 text-[10px] font-bold px-2.5 py-1 rounded-full shadow-xs ${p.stockUnits > 0 ? 'bg-emerald-50 text-emerald-700 border border-emerald-200/90' : 'bg-slate-100 text-slate-600 border border-slate-200/90'}">
          <span class="w-2 h-2 rounded-full ${p.stockUnits > 0 ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400'}"></span>
          <span>${p.stockUnits > 0 ? 'Hay stock' : 'Sin stock'}</span>
        </span>

        <!-- Supplier tag if available -->
        ${p.supplier ? `
          <span class="absolute bottom-2 left-2 text-[9px] font-medium bg-white/90 backdrop-blur px-2 py-0.5 rounded shadow-xs text-slate-600 border border-slate-200/60">
            ${p.supplier}
          </span>
        ` : ''}
      </div>

      <div class="p-4 flex-1 flex flex-col">
        <!-- Badges & SKU -->
        <div class="flex items-center justify-between gap-2 mb-1.5">
          <span class="text-[10px] font-bold tracking-wider uppercase px-2 py-0.5 rounded-md bg-slate-100 text-slate-600">
            SKU: ${p.sku}
          </span>
          ${p.supplierCode ? `
            <span class="text-[10px] font-mono text-slate-400">
              Cód: ${p.supplierCode}
            </span>
          ` : ''}
        </div>

        <!-- Title -->
        <h3 class="text-xs font-bold text-slate-900 group-hover:text-amber-600 transition-colors line-clamp-2 mb-2 leading-snug">
          ${p.name}
        </h3>

        <!-- Specification Badges (Medidas, Capacidad, Pack) -->
        <div class="flex flex-wrap gap-1.5 mb-3">
          ${p.dimensions ? `
            <span class="inline-flex items-center gap-1 text-[10px] font-bold bg-amber-50 text-amber-800 px-2 py-0.5 rounded-md border border-amber-200/60">
              <span>📏</span> ${p.dimensions}
            </span>
          ` : ''}
          ${p.capacity ? `
            <span class="inline-flex items-center gap-1 text-[10px] font-bold bg-sky-50 text-sky-800 px-2 py-0.5 rounded-md border border-sky-200/60">
              <span>💧</span> ${p.capacity}
            </span>
          ` : ''}
          ${p.packageQuantity ? `
            <span class="inline-flex items-center gap-1 text-[10px] font-semibold bg-slate-100 text-slate-700 px-2 py-0.5 rounded-md">
              <span>📦</span> ${p.packageQuantity}
            </span>
          ` : ''}
        </div>

        <!-- Precios de Lista (Lista 1 Minorista & Lista 2 Mayorista) -->
        <div class="bg-slate-50/90 rounded-xl p-2.5 mb-3 border border-slate-200/80 space-y-1.5">
          <div class="flex items-center justify-between text-xs">
            <span class="text-slate-600 font-semibold">Lista 1 (Minorista):</span>
            <span class="font-bold text-slate-900">${formatCurrency(p.basePrice)}</span>
          </div>
          <div class="flex items-center justify-between text-xs">
            <span class="text-amber-700 font-bold flex items-center gap-1">
              <i data-lucide="tag" class="w-3.5 h-3.5"></i>
              <span>Lista 2 (Mayorista):</span>
            </span>
            <span class="font-black text-amber-700 text-sm">${formatCurrency(p.wholesalePrice)}</span>
          </div>
          ${currentSpecialClient && currentSpecialClient.discountPercentage > 0 ? `
            <div class="flex items-center justify-between text-xs pt-1.5 border-t border-amber-300/80 bg-amber-100/80 -mx-2.5 -mb-2.5 px-2.5 py-1.5 rounded-b-xl">
              <span class="text-amber-950 font-bold flex items-center gap-1">
                <span>🌟</span> Tu Precio (${currentSpecialClient.discountPercentage}% OFF):
              </span>
              <span class="font-black text-emerald-700 text-sm">
                ${formatCurrency(p.basePrice * (1 - currentSpecialClient.discountPercentage / 100))}
              </span>
            </div>
          ` : `
            <div class="text-[10px] text-slate-400 pt-1 border-t border-slate-200/70 flex items-center justify-between">
              <span>Descuento se aplica en comprobante</span>
              <span class="font-bold text-amber-600">Mín: ${formatCurrency(commercialSettings.minPurchaseForDiscount)}</span>
            </div>
          `}
        </div>

        <!-- Quantity Controls & Add to Quote -->
        <div class="space-y-2">
          <div class="flex items-center gap-1.5">
            <div class="flex items-center border border-slate-200 rounded-xl bg-slate-50 overflow-hidden w-28 shrink-0">
              <button onclick="adjustQuantity('${p.id}', -1)" class="w-8 h-8 flex items-center justify-center text-slate-600 hover:bg-white active:bg-slate-200 transition font-bold text-sm">-</button>
              <input 
                type="number" 
                id="qty-input-${p.id}" 
                value="${sel.quantity}" 
                min="1" 
                onchange="onQuantityDirectChange('${p.id}', this.value)" 
                class="w-12 text-center text-xs font-bold bg-transparent border-0 outline-none p-0"
              />
              <button onclick="adjustQuantity('${p.id}', 1)" class="w-8 h-8 flex items-center justify-center text-slate-600 hover:bg-white active:bg-slate-200 transition font-bold text-sm">+</button>
            </div>

            <button onclick="addToQuote('${p.id}')" class="flex-1 bg-slate-900 hover:bg-amber-600 active:bg-amber-700 text-white font-bold text-xs py-2 px-3 rounded-xl shadow-xs transition-all flex items-center justify-center gap-1.5">
              <i data-lucide="plus" class="w-3.5 h-3.5"></i>
              <span>Añadir</span>
            </button>
          </div>
        </div>

      </div>
    `;

    grid.appendChild(card);
  });

  renderPagination(totalProducts, totalPages, startIndex + 1, endIndex);
  lucide.createIcons();
}

function renderPagination(totalProducts, totalPages, startItem, endItem) {
  const paginationContainer = document.getElementById('catalogPagination');
  if (!paginationContainer) return;

  if (totalPages <= 1) {
    paginationContainer.classList.add('hidden');
    return;
  }
  paginationContainer.classList.remove('hidden');

  const rangeText = document.getElementById('paginationRangeText');
  const totalText = document.getElementById('paginationTotalText');
  const currentText = document.getElementById('paginationCurrentText');
  const pagesText = document.getElementById('paginationPagesText');

  if (rangeText) rangeText.textContent = `${startItem} - ${endItem}`;
  if (totalText) totalText.textContent = totalProducts;
  if (currentText) currentText.textContent = catalogCurrentPage;
  if (pagesText) pagesText.textContent = totalPages;

  const prevBtn = document.getElementById('btnPrevPage');
  const nextBtn = document.getElementById('btnNextPage');
  if (prevBtn) prevBtn.disabled = (catalogCurrentPage <= 1);
  if (nextBtn) nextBtn.disabled = (catalogCurrentPage >= totalPages);

  const btnsContainer = document.getElementById('paginationButtons');
  if (!btnsContainer) return;
  btnsContainer.innerHTML = '';

  const pagesToShow = getPaginationRange(catalogCurrentPage, totalPages);

  pagesToShow.forEach(item => {
    if (item === '...') {
      const dots = document.createElement('span');
      dots.className = "px-2 py-1 text-xs text-slate-400 font-bold select-none";
      dots.textContent = '...';
      btnsContainer.appendChild(dots);
    } else {
      const pageNum = item;
      const btn = document.createElement('button');
      btn.textContent = pageNum;
      btn.onclick = () => changeCatalogPage(pageNum);
      if (pageNum === catalogCurrentPage) {
        btn.className = "w-8 h-8 rounded-xl text-xs font-bold bg-slate-900 text-white shadow-xs transition";
      } else {
        btn.className = "w-8 h-8 rounded-xl text-xs font-semibold bg-white border border-slate-200 text-slate-700 hover:bg-slate-100 transition";
      }
      btnsContainer.appendChild(btn);
    }
  });
}

function getPaginationRange(current, total) {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }
  const pages = [];
  pages.push(1);
  if (current > 3) {
    pages.push('...');
  }
  const start = Math.max(2, current - 1);
  const end = Math.min(total - 1, current + 1);
  for (let i = start; i <= end; i++) {
    pages.push(i);
  }
  if (current < total - 2) {
    pages.push('...');
  }
  pages.push(total);
  return pages;
}

function changeCatalogPage(newPage) {
  const totalPages = Math.ceil(products.length / catalogPageSize);
  if (newPage < 1 || newPage > totalPages || newPage === catalogCurrentPage) return;
  catalogCurrentPage = newPage;
  renderProductGrid();

  const filterBar = document.getElementById('catalogFilterBar');
  if (filterBar) {
    filterBar.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}

function getSourceBadgeClass(source) {
  switch (source) {
    case 'TierPrice': return 'bg-purple-100 text-purple-700 border border-purple-200';
    case 'CustomerPrice': return 'bg-emerald-100 text-emerald-700 border border-emerald-200';
    case 'GroupPrice': return 'bg-amber-100 text-amber-700 border border-amber-200';
    default: return 'hidden';
  }
}

function getSourceBadgeLabel(source) {
  switch (source) {
    case 'TierPrice': return 'Escala Volumen';
    case 'CustomerPrice': return 'Precio Acordado';
    case 'GroupPrice': return 'P. Mayorista';
    default: return '';
  }
}

// Adjust quantity & recalculate live price
async function adjustQuantity(productId, delta) {
  const sel = productSelections[productId];
  if (!sel) return;

  const newQty = Math.max(1, sel.quantity + delta);
  sel.quantity = newQty;
  document.getElementById(`qty-input-${productId}`).value = newQty;

  await recalculateLivePrice(productId);
}

async function onQuantityDirectChange(productId, val) {
  const sel = productSelections[productId];
  if (!sel) return;

  const parsed = parseInt(val, 10);
  sel.quantity = isNaN(parsed) || parsed < 1 ? 1 : parsed;
  await recalculateLivePrice(productId);
}

async function recalculateLivePrice(productId) {
  const p = products.find(prod => prod.id === productId);
  const sel = productSelections[productId];
  if (!p || !sel) return;

  try {
    const res = await fetch(`${API_BASE}/pricing/calculate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        productId: p.id,
        quantity: sel.quantity,
        customerId: selectedCustomerId || null,
        customerGroupId: selectedGroupId || null
      })
    });

    if (res.ok) {
      const data = await res.json();
      sel.calculatedPrice = data.unitPrice;
      sel.priceSource = data.appliedSource;
      sel.savingsPercentage = data.savingsPercentage;

      const priceDisplay = document.getElementById(`price-display-${p.id}`);
      if (priceDisplay) priceDisplay.textContent = formatCurrency(data.unitPrice);

      const badge = document.getElementById(`source-badge-${p.id}`);
      if (badge) {
        badge.className = `text-[10px] font-bold px-2 py-0.5 rounded-full ${getSourceBadgeClass(data.appliedSource)}`;
        badge.textContent = getSourceBadgeLabel(data.appliedSource);
      }
    }
  } catch (err) {
    console.error('Error calculando precio dinámico:', err);
  }
}

// ==========================================
// 3. VISTA GESTIÓN DE ARTÍCULOS (ABM)
// ==========================================

async function loadAdminStats() {
  try {
    const res = await fetch(`${API_BASE}/admin/stats`);
    if (res.ok) {
      const stats = await res.json();
      document.getElementById('statTotal').textContent = stats.totalProducts;
      document.getElementById('statActive').textContent = stats.activeProducts;
      document.getElementById('statPerfumery').textContent = stats.perfumeryCount;
      document.getElementById('statMissingImages').textContent = stats.missingImages;
      document.getElementById('statMissingDimensions').textContent = stats.missingDimensions;
      document.getElementById('statInactive').textContent = stats.inactiveProducts;

      const navBadge = document.getElementById('navStatsBadge');
      if (navBadge) navBadge.textContent = stats.totalProducts;
    }
  } catch (err) {
    console.error('Error al cargar stats:', err);
  }
}

let adminSearchDebounceTimer = null;
function onAdminSearchInput() {
  clearTimeout(adminSearchDebounceTimer);
  adminSearchDebounceTimer = setTimeout(() => {
    currentAdminPage = 1;
    loadAdminProducts();
  }, 250);
}

function setAdminFilter(filterName) {
  activeAdminFilter = filterName;
  currentAdminPage = 1;

  // Actualizar botones de filtro
  document.querySelectorAll('#adminFilterTabs button').forEach(btn => {
    btn.className = "px-3 py-1.5 text-xs font-semibold rounded-lg bg-slate-100 text-slate-700 hover:bg-slate-200 transition";
  });

  const activeBtn = document.getElementById(`btnFilter-${filterName}`);
  if (activeBtn) {
    activeBtn.className = "px-3 py-1.5 text-xs font-bold rounded-lg bg-slate-900 text-white transition";
  }

  loadAdminProducts();
}

async function loadAdminProducts() {
  const search = document.getElementById('adminSearchInput').value.trim();
  let url = `${API_BASE}/admin/products?filterType=${activeAdminFilter}&`;
  if (search) url += `search=${encodeURIComponent(search)}&`;

  try {
    const res = await fetch(url);
    if (res.ok) {
      adminProducts = await res.json();
      renderAdminTable();
    }
  } catch (err) {
    console.error('Error al cargar productos en ABM:', err);
  }
}

function renderAdminTable() {
  const tbody = document.getElementById('adminTableBody');
  const countLabel = document.getElementById('adminTableCount');
  tbody.innerHTML = '';

  const total = adminProducts.length;
  const startIdx = (currentAdminPage - 1) * ADMIN_PAGE_SIZE;
  const pageItems = adminProducts.slice(startIdx, startIdx + ADMIN_PAGE_SIZE);

  countLabel.textContent = `Mostrando ${Math.min(startIdx + 1, total)} - ${Math.min(startIdx + ADMIN_PAGE_SIZE, total)} de ${total} artículos`;

  if (pageItems.length === 0) {
    tbody.innerHTML = `
      <tr>
        <td colspan="10" class="py-12 text-center text-slate-400">
          <i data-lucide="package-search" class="w-8 h-8 mx-auto mb-2 opacity-50"></i>
          <span>No se encontraron artículos con el filtro actual.</span>
        </td>
      </tr>
    `;
    lucide.createIcons();
    return;
  }

  pageItems.forEach(p => {
    const tr = document.createElement('tr');
    tr.className = `hover:bg-slate-50/80 transition-colors ${!p.isActive ? 'bg-slate-50/50 text-slate-400' : ''}`;

    const catIcon = getCategoryIcon(p.categoryName);

    tr.innerHTML = `
      <!-- Foto / Miniatura con subida directa -->
      <td class="py-2.5 px-4 text-center">
        <div class="relative w-10 h-10 rounded-lg border border-slate-200 bg-white overflow-hidden mx-auto flex items-center justify-center group/img">
          ${p.imageUrl ? `
            <img src="${p.imageUrl}" alt="${p.name}" class="w-full h-full object-cover" />
          ` : `
            <i data-lucide="${catIcon}" class="w-5 h-5 text-slate-300"></i>
          `}
          <label title="Subir foto rápida" class="absolute inset-0 bg-slate-900/60 text-white flex items-center justify-center opacity-0 group-hover/img:opacity-100 cursor-pointer transition">
            <i data-lucide="camera" class="w-3.5 h-3.5"></i>
            <input type="file" accept="image/*" onchange="uploadDirectImage('${p.id}', event)" class="hidden" />
          </label>
        </div>
      </td>

      <!-- SKU y Cód. Prov -->
      <td class="py-2.5 px-4 font-mono">
        <span class="font-bold text-slate-900 block">${p.sku}</span>
        ${p.supplierCode ? `<span class="text-[10px] text-slate-400">${p.supplierCode}</span>` : ''}
      </td>

      <!-- Nombre & Categoría -->
      <td class="py-2.5 px-4 max-w-xs">
        <span class="font-bold text-slate-800 line-clamp-1 block" title="${p.name}">${p.name}</span>
        <div class="flex items-center gap-1.5 mt-0.5">
          <span class="text-[10px] text-amber-700 bg-amber-50 px-1.5 py-0.2 rounded font-medium">${p.categoryName}</span>
          ${p.supplier ? `<span class="text-[10px] text-slate-400">· ${p.supplier}</span>` : ''}
        </div>
      </td>

      <!-- Medidas & Especificaciones -->
      <td class="py-2.5 px-4">
        <div class="flex flex-wrap gap-1">
          ${p.dimensions ? `<span class="text-[10px] font-bold bg-amber-100/70 text-amber-900 px-1.5 py-0.2 rounded">📏 ${p.dimensions}</span>` : '<span class="text-[10px] text-rose-500 italic">Sin medida</span>'}
          ${p.capacity ? `<span class="text-[10px] font-bold bg-sky-100/70 text-sky-900 px-1.5 py-0.2 rounded">💧 ${p.capacity}</span>` : ''}
          ${p.packageQuantity ? `<span class="text-[10px] bg-slate-100 text-slate-700 px-1.5 py-0.2 rounded">📦 ${p.packageQuantity}</span>` : ''}
        </div>
      </td>

      <!-- P. Costo -->
      <td class="py-2.5 px-4 text-right font-mono text-slate-500">
        ${formatCurrency(p.costPrice)}
      </td>

      <!-- P. Venta 1 -->
      <td class="py-2.5 px-4 text-right font-mono font-bold text-slate-900">
        ${formatCurrency(p.basePrice)}
      </td>

      <!-- P. Venta 2 -->
      <td class="py-2.5 px-4 text-right font-mono font-bold text-amber-600">
        ${formatCurrency(p.wholesalePrice)}
      </td>

      <!-- Stock -->
      <td class="py-2.5 px-4 text-center">
        <span class="px-2 py-0.5 rounded-full font-bold text-[10px] ${p.stockUnits > 0 ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-600'}">
          ${p.stockUnits}
        </span>
      </td>

      <!-- Switch Activo / Inactivo -->
      <td class="py-2.5 px-4 text-center">
        <button onclick="toggleProductActive('${p.id}')" class="text-xs px-2.5 py-1 rounded-full font-bold transition flex items-center justify-center gap-1 mx-auto ${p.isActive ? 'bg-emerald-100 text-emerald-800 hover:bg-emerald-200' : 'bg-slate-200 text-slate-600 hover:bg-slate-300'}">
          <span class="w-1.5 h-1.5 rounded-full ${p.isActive ? 'bg-emerald-600' : 'bg-slate-400'}"></span>
          <span>${p.isActive ? 'Activo' : 'Inactivo'}</span>
        </button>
      </td>

      <!-- Acciones -->
      <td class="py-2.5 px-4 text-center">
        <div class="flex items-center justify-center gap-1">
          <button onclick="openProductModal('${p.id}')" title="Editar artículo" class="p-1.5 text-slate-500 hover:text-amber-600 hover:bg-amber-50 rounded-lg transition">
            <i data-lucide="edit-3" class="w-4 h-4"></i>
          </button>
          <button onclick="deleteProduct('${p.id}', '${p.name.replace(/'/g, "\\'")}')" title="Eliminar" class="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition">
            <i data-lucide="trash-2" class="w-4 h-4"></i>
          </button>
        </div>
      </td>
    `;

    tbody.appendChild(tr);
  });

  renderAdminPagination(total);
  lucide.createIcons();
}

function renderAdminPagination(total) {
  const container = document.getElementById('adminPagination');
  container.innerHTML = '';
  const totalPages = Math.ceil(total / ADMIN_PAGE_SIZE);
  if (totalPages <= 1) return;

  for (let i = 1; i <= Math.min(totalPages, 7); i++) {
    const btn = document.createElement('button');
    btn.className = `px-2.5 py-1 rounded-lg text-xs font-bold transition ${i === currentAdminPage ? 'bg-amber-500 text-white' : 'bg-white border border-slate-200 text-slate-700 hover:bg-slate-100'}`;
    btn.textContent = i;
    btn.onclick = () => {
      currentAdminPage = i;
      renderAdminTable();
    };
    container.appendChild(btn);
  }
}

// Toggle Activo/Inactivo
async function toggleProductActive(productId) {
  try {
    const res = await fetch(`${API_BASE}/admin/products/${productId}/toggle-status`, { method: 'PATCH' });
    if (res.ok) {
      const data = await res.json();
      const p = adminProducts.find(item => item.id === productId);
      if (p) p.isActive = data.isActive;
      renderAdminTable();
      loadAdminStats();
      showToast(`Estado actualizado: ${data.isActive ? 'Activo' : 'Inactivo'}`);
    }
  } catch (err) {
    showToast('Error al cambiar estado', false);
  }
}

// Direct File Upload from Table
async function uploadDirectImage(productId, event) {
  const file = event.target.files[0];
  if (!file) return;

  const formData = new FormData();
  formData.append('file', file);

  try {
    const res = await fetch(`${API_BASE}/admin/products/${productId}/image`, {
      method: 'POST',
      body: formData
    });

    if (res.ok) {
      const data = await res.json();
      const p = adminProducts.find(item => item.id === productId);
      if (p) p.imageUrl = data.imageUrl;
      renderAdminTable();
      loadAdminStats();
      showToast('¡Foto subida con éxito!');
    } else {
      showToast('Error al subir la imagen', false);
    }
  } catch (err) {
    showToast('Error al subir imagen', false);
  }
}

// ==========================================
// 4. MODAL DE EDICIÓN / ALTA DE PRODUCTO
// ==========================================

let pendingUploadFile = null;

function openProductModal(productId) {
  const modal = document.getElementById('productModal');
  const modalTitle = document.getElementById('modalProductTitle');
  pendingUploadFile = null;
  populateCategorySelects();

  if (productId) {
    const p = adminProducts.find(item => item.id === productId);
    if (!p) return;

    modalTitle.textContent = "Editar Artículo";
    document.getElementById('editProductId').value = p.id;
    document.getElementById('editSku').value = p.sku;
    document.getElementById('editSupplierCode').value = p.supplierCode || '';
    document.getElementById('editName').value = p.name;
    document.getElementById('editCategoryId').value = p.categoryId;
    document.getElementById('editDimensions').value = p.dimensions || '';
    document.getElementById('editCapacity').value = p.capacity || '';
    document.getElementById('editPackageQuantity').value = p.packageQuantity || '';
    document.getElementById('editSupplier').value = p.supplier || '';
    document.getElementById('editCostPrice').value = p.costPrice || 0;
    document.getElementById('editBasePrice').value = p.basePrice || 0;
    document.getElementById('editWholesalePrice').value = p.wholesalePrice || p.basePrice || 0;
    document.getElementById('editStockUnits').value = p.stockUnits || 0;
    document.getElementById('editNotes').value = p.notes || '';
    document.getElementById('editIsActive').checked = p.isActive;
    document.getElementById('editImageUrl').value = p.imageUrl || '';

    updateImagePreview(p.imageUrl);
  } else {
    modalTitle.textContent = "Nuevo Artículo";
    document.getElementById('productForm').reset();
    document.getElementById('editProductId').value = '';
    document.getElementById('editIsActive').checked = true;
    updateImagePreview(null);
  }

  modal.classList.remove('hidden');
  lucide.createIcons();
}

function closeProductModal() {
  document.getElementById('productModal').classList.add('hidden');
  pendingUploadFile = null;
}

function updateImagePreview(url) {
  const img = document.getElementById('imagePreview');
  const placeholder = document.getElementById('imagePlaceholder');
  if (url) {
    img.src = url;
    img.classList.remove('hidden');
    placeholder.classList.add('hidden');
  } else {
    img.src = '';
    img.classList.add('hidden');
    placeholder.classList.remove('hidden');
  }
}

function onFileSelected(event) {
  const file = event.target.files[0];
  if (!file) return;

  pendingUploadFile = file;
  const objectUrl = URL.createObjectURL(file);
  updateImagePreview(objectUrl);
}

function onUrlInput(url) {
  updateImagePreview(url.trim());
}

// Asistente Inteligente de Auto-Extracción de Atributos
async function autoDetectAttributes() {
  const title = document.getElementById('editName').value.trim();
  const supplier = document.getElementById('editSupplier').value.trim();

  if (!title) {
    showToast('Ingresa un título para extraer las especificaciones', false);
    return;
  }

  try {
    const res = await fetch(`${API_BASE}/admin/products/parse-attributes`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title, supplier })
    });

    if (res.ok) {
      const data = await res.json();
      if (data.cleanName) document.getElementById('editName').value = data.cleanName;
      if (data.dimensions) document.getElementById('editDimensions').value = data.dimensions;
      if (data.capacity) document.getElementById('editCapacity').value = data.capacity;
      if (data.packageQuantity) document.getElementById('editPackageQuantity').value = data.packageQuantity;

      // Buscar categoría sugerida
      if (data.suggestedCategory) {
        const cat = categories.find(c => c.name.toLowerCase() === data.suggestedCategory.toLowerCase());
        if (cat) {
          document.getElementById('editCategoryId').value = cat.id;
        }
      }

      showToast('¡Especificaciones auto-detectadas con éxito!');
    }
  } catch (err) {
    showToast('Error al auto-detectar especificaciones', false);
  }
}

// Guardar Producto (Alta o Edición)
async function saveProduct(event) {
  event.preventDefault();
  const btn = document.getElementById('btnSaveProduct');
  btn.disabled = true;

  const id = document.getElementById('editProductId').value;
  const isEdit = Boolean(id);

  const payload = {
    categoryId: document.getElementById('editCategoryId').value,
    sku: document.getElementById('editSku').value.trim(),
    supplierCode: document.getElementById('editSupplierCode').value.trim() || null,
    name: document.getElementById('editName').value.trim(),
    description: '',
    dimensions: document.getElementById('editDimensions').value.trim() || null,
    capacity: document.getElementById('editCapacity').value.trim() || null,
    packageQuantity: document.getElementById('editPackageQuantity').value.trim() || null,
    supplier: document.getElementById('editSupplier').value.trim() || null,
    notes: document.getElementById('editNotes').value.trim() || null,
    costPrice: parseFloat(document.getElementById('editCostPrice').value) || 0,
    basePrice: parseFloat(document.getElementById('editBasePrice').value) || 0,
    wholesalePrice: parseFloat(document.getElementById('editWholesalePrice').value) || 0,
    stockUnits: parseInt(document.getElementById('editStockUnits').value, 10) || 0,
    isActive: document.getElementById('editIsActive').checked,
    imageUrl: document.getElementById('editImageUrl').value.trim() || null
  };

  try {
    const url = isEdit ? `${API_BASE}/admin/products/${id}` : `${API_BASE}/admin/products`;
    const method = isEdit ? 'PUT' : 'POST';

    const res = await fetch(url, {
      method,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (res.ok) {
      const savedProd = await res.json();
      const targetId = isEdit ? id : savedProd.id;

      // Si hay archivo de imagen pendiente para subir
      if (pendingUploadFile && targetId) {
        const formData = new FormData();
        formData.append('file', pendingUploadFile);
        await fetch(`${API_BASE}/admin/products/${targetId}/image`, {
          method: 'POST',
          body: formData
        });
      }

      closeProductModal();
      await loadAdminProducts();
      await loadAdminStats();
      showToast(isEdit ? '¡Artículo actualizado correctamente!' : '¡Artículo creado exitosamente!');
    } else {
      const errData = await res.json().catch(() => ({}));
      showToast(errData.message || 'Error al guardar el artículo', false);
    }
  } catch (err) {
    showToast('Error de red al guardar el artículo', false);
  } finally {
    btn.disabled = false;
  }
}

// Eliminar Producto
async function deleteProduct(productId, name) {
  if (!confirm(`¿Estás seguro de que deseas eliminar "${name}" del catálogo?`)) {
    return;
  }

  try {
    const res = await fetch(`${API_BASE}/admin/products/${productId}`, { method: 'DELETE' });
    if (res.ok) {
      adminProducts = adminProducts.filter(p => p.id !== productId);
      renderAdminTable();
      loadAdminStats();
      showToast('Artículo eliminado.');
    } else {
      showToast('No se pudo eliminar el artículo', false);
    }
  } catch (err) {
    showToast('Error de red al eliminar', false);
  }
}

// Re-sincronizar CSV
async function reimportCsv() {
  if (!confirm('¿Deseas volver a sincronizar la base completa desde el archivo CSV articulos_gasbel.csv?')) {
    return;
  }

  try {
    showToast('Sincronizando base de artículos...');
    const res = await fetch(`${API_BASE}/admin/import-csv`, { method: 'POST' });
    if (res.ok) {
      const data = await res.json();
      await loadCategories();
      await loadAdminStats();
      await loadAdminProducts();
      await loadProducts();
      showToast(data.message);
    } else {
      showToast('Error al importar el archivo CSV', false);
    }
  } catch (err) {
    showToast('Error de red al importar CSV', false);
  }
}

// ==========================================
// 5. COTIZADOR, REGLAS DE DESCUENTO Y WHATSAPP
// ==========================================

let isDrawerMaximized = false;

function toggleQuoteDrawer() {
  const drawer = document.getElementById('quoteDrawer');
  drawer.classList.toggle('hidden');
  renderQuoteDrawer();
}

function toggleDrawerMaximize() {
  isDrawerMaximized = !isDrawerMaximized;
  applyDrawerMaximizeState();
}

function applyDrawerMaximizeState() {
  const wrapper = document.getElementById('quoteDrawerWrapper');
  const panel = document.getElementById('quoteDrawerPanel');
  const headerInner = document.getElementById('drawerHeaderInner');
  const grid = document.getElementById('quoteDrawerGrid');
  const colLeft = document.getElementById('quoteDrawerColLeft');
  const colRight = document.getElementById('quoteDrawerColRight');
  const footerContent = document.getElementById('drawerFooterContent');
  const badge = document.getElementById('drawerFullscreenBadge');
  const btn = document.getElementById('btnMaximizeDrawer');

  if (isDrawerMaximized) {
    // Modo Pantalla Completa
    wrapper?.classList.remove('pl-6', 'sm:pl-10');
    wrapper?.classList.add('pl-0');

    panel?.classList.remove('max-w-md');
    panel?.classList.add('max-w-full');

    headerInner?.classList.add('max-w-7xl', 'mx-auto');

    grid?.classList.remove('space-y-4');
    grid?.classList.add('max-w-7xl', 'mx-auto', 'w-full', 'grid', 'grid-cols-1', 'lg:grid-cols-12', 'gap-6');

    colLeft?.classList.add('lg:col-span-7', 'xl:col-span-8');
    colRight?.classList.add('lg:col-span-5', 'xl:col-span-4');

    footerContent?.classList.remove('space-y-2.5');
    footerContent?.classList.add('max-w-7xl', 'mx-auto', 'flex', 'flex-col', 'sm:flex-row', 'sm:items-center', 'sm:justify-between', 'gap-4');

    if (badge) badge.classList.remove('hidden');
    if (btn) {
      btn.title = 'Restaurar a panel lateral';
      btn.innerHTML = `
        <i data-lucide="minimize-2" id="iconMaximizeDrawer" class="w-4 h-4 text-amber-600"></i>
        <span id="textMaximizeDrawer" class="hidden sm:inline text-xs font-semibold text-slate-700">Reducir</span>
      `;
    }
  } else {
    // Modo Panel Lateral
    wrapper?.classList.add('pl-6', 'sm:pl-10');
    wrapper?.classList.remove('pl-0');

    panel?.classList.add('max-w-md');
    panel?.classList.remove('max-w-full');

    headerInner?.classList.remove('max-w-7xl', 'mx-auto');

    grid?.classList.add('space-y-4');
    grid?.classList.remove('max-w-7xl', 'mx-auto', 'w-full', 'grid', 'grid-cols-1', 'lg:grid-cols-12', 'gap-6');

    colLeft?.classList.remove('lg:col-span-7', 'xl:col-span-8');
    colRight?.classList.remove('lg:col-span-5', 'xl:col-span-4');

    footerContent?.classList.add('space-y-2.5');
    footerContent?.classList.remove('max-w-7xl', 'mx-auto', 'flex', 'flex-col', 'sm:flex-row', 'sm:items-center', 'sm:justify-between', 'gap-4');

    if (badge) badge.classList.add('hidden');
    if (btn) {
      btn.title = 'Agrandar a página completa';
      btn.innerHTML = `
        <i data-lucide="maximize-2" id="iconMaximizeDrawer" class="w-4 h-4 text-amber-600"></i>
        <span id="textMaximizeDrawer" class="hidden sm:inline text-xs font-semibold text-slate-700">Agrandar</span>
      `;
    }
  }

  lucide.createIcons();
}

function onDeliveryToggle() {
  updateDrawerTotals();
}

function onCustomerNameChanged(name) {
  const trimmed = (name || '').trim().toLowerCase();
  const match = specialClients.find(c => c.isActive && c.name.toLowerCase() === trimmed);
  const badge = document.getElementById('specialClientBadge');
  const badgeText = document.getElementById('specialClientBadgeText');

  if (match) {
    currentSpecialClient = match;
    if (badge && badgeText) {
      badge.classList.remove('hidden');
      badgeText.textContent = `🌟 Cliente Especial identificado: ${match.name} — ¡${match.discountPercentage}% OFF asignado!`;
    }
    const addrInput = document.getElementById('quoteCustomerAddress');
    const phoneInput = document.getElementById('quoteCustomerPhone');
    if (addrInput && !addrInput.value && match.address) addrInput.value = match.address;
    if (phoneInput && !phoneInput.value && match.phone) phoneInput.value = match.phone;
  } else {
    currentSpecialClient = null;
    if (badge) badge.classList.add('hidden');
  }

  updateDrawerTotals();
}

function addToQuote(productId) {
  const p = products.find(prod => prod.id === productId);
  const sel = productSelections[productId];
  if (!p || !sel) return;

  const isBox = sel.packaging === 'Box';
  const effectiveUnits = isBox ? sel.quantity * (p.unitsPerBox > 0 ? p.unitsPerBox : 1) : sel.quantity;
  const unitPrice = p.basePrice; // Lista 1 (Minorista)
  const wholesalePrice = p.wholesalePrice > 0 ? p.wholesalePrice : p.basePrice; // Lista 2 (Mayorista)
  const lineSubtotal = Math.round(unitPrice * effectiveUnits * 100) / 100;
  const lineWholesaleSubtotal = Math.round(wholesalePrice * effectiveUnits * 100) / 100;

  const existingIdx = quoteItems.findIndex(item => item.product.id === productId && item.packagingSelected === sel.packaging);

  if (existingIdx >= 0) {
    quoteItems[existingIdx].quantity += sel.quantity;
    quoteItems[existingIdx].effectiveUnits += effectiveUnits;
    quoteItems[existingIdx].lineSubtotal = Math.round(unitPrice * quoteItems[existingIdx].effectiveUnits * 100) / 100;
    quoteItems[existingIdx].lineWholesaleSubtotal = Math.round(wholesalePrice * quoteItems[existingIdx].effectiveUnits * 100) / 100;
  } else {
    quoteItems.push({
      product: p,
      quantity: sel.quantity,
      packagingSelected: sel.packaging,
      effectiveUnits: effectiveUnits,
      unitPrice: unitPrice,
      wholesalePrice: wholesalePrice,
      lineSubtotal: lineSubtotal,
      lineWholesaleSubtotal: lineWholesaleSubtotal
    });
  }

  updateCartBadge();
  showToast(`Añadido al pedido: ${p.name}`);
  renderQuoteDrawer();
}

function updateCartBadge() {
  const count = quoteItems.reduce((acc, item) => acc + item.quantity, 0);
  const badge = document.getElementById('cartCountBadge');
  if (badge) badge.textContent = count;
}

function recalculateQuoteItems() {
  quoteItems.forEach(item => {
    const isBox = item.packagingSelected === 'Box';
    item.effectiveUnits = isBox ? item.quantity * (item.product.unitsPerBox > 0 ? item.product.unitsPerBox : 1) : item.quantity;
    item.unitPrice = item.product.basePrice;
    item.wholesalePrice = item.product.wholesalePrice > 0 ? item.product.wholesalePrice : item.product.basePrice;
    item.lineSubtotal = Math.round(item.unitPrice * item.effectiveUnits * 100) / 100;
    item.lineWholesaleSubtotal = Math.round(item.wholesalePrice * item.effectiveUnits * 100) / 100;
  });
  renderQuoteDrawer();
}

function renderQuoteDrawer() {
  const list = document.getElementById('quoteItemsList');
  if (!list) return;
  list.innerHTML = '';

  const itemsCountBadge = document.getElementById('drawerItemsCountBadge');
  if (itemsCountBadge) itemsCountBadge.textContent = quoteItems.length;

  if (quoteItems.length === 0) {
    list.innerHTML = `
      <div class="text-center py-12 text-slate-400">
        <i data-lucide="shopping-cart" class="w-10 h-10 mx-auto mb-2 opacity-40"></i>
        <p class="text-xs">No hay artículos en la cotización actual.</p>
      </div>
    `;
    updateDrawerTotals();
    lucide.createIcons();
    return;
  }

  quoteItems.forEach((item, index) => {
    const units = item.effectiveUnits || item.quantity;
    const div = document.createElement('div');
    div.className = "py-3 flex items-center justify-between gap-3";
    div.innerHTML = `
      <div class="flex-1 min-w-0">
        <h4 class="text-xs font-bold text-slate-800 line-clamp-1">${item.product.name}</h4>
        <div class="flex items-center gap-2 text-[11px] text-slate-500 mt-0.5">
          <span>${units} u. (${item.packagingSelected === 'Box' ? 'Bulto' : 'Unidad'}) x ${formatCurrency(item.unitPrice)}</span>
          <span class="text-[10px] font-semibold text-slate-600 bg-slate-100 px-1 rounded">Lista 1</span>
        </div>
      </div>
      <div class="text-right shrink-0">
        <span class="text-xs font-bold text-slate-900 block">${formatCurrency(item.lineSubtotal)}</span>
        <button onclick="removeQuoteItem(${index})" class="text-[11px] text-rose-500 hover:text-rose-700 font-semibold mt-0.5">Quitar</button>
      </div>
    `;
    list.appendChild(div);
  });

  updateDrawerTotals();
  lucide.createIcons();
}

function removeQuoteItem(index) {
  quoteItems.splice(index, 1);
  updateCartBadge();
  renderQuoteDrawer();
}

function updateDrawerTotals() {
  const baseSubtotal = quoteItems.reduce((acc, item) => acc + item.lineSubtotal, 0);
  const wholesaleSubtotal = quoteItems.reduce((acc, item) => acc + item.lineWholesaleSubtotal, 0);

  const minDiscount = commercialSettings.minPurchaseForDiscount || 50000;
  const minFreeShipping = commercialSettings.minPurchaseForFreeShipping || 100000;

  let discountAmount = 0;
  let discountLabel = "Descuento Mayorista (Lista 2):";

  const discountNoticeEl = document.getElementById('quoteDiscountNotice');
  const discountNoticeTextEl = document.getElementById('quoteDiscountNoticeText');
  const discountRowEl = document.getElementById('drawerDiscountRow');
  const discountAmountEl = document.getElementById('drawerDiscountAmount');
  const discountLabelEl = document.getElementById('drawerDiscountLabel');

  const qualifiesForDiscount = baseSubtotal >= minDiscount && baseSubtotal > 0;

  if (qualifiesForDiscount) {
    if (currentSpecialClient && currentSpecialClient.discountPercentage > 0) {
      discountAmount = Math.round(baseSubtotal * (currentSpecialClient.discountPercentage / 100) * 100) / 100;
      discountLabel = `Descuento Especial (${currentSpecialClient.discountPercentage}% OFF):`;
    } else {
      discountAmount = Math.max(0, Math.round((baseSubtotal - wholesaleSubtotal) * 100) / 100);
      discountLabel = "Descuento Mayorista (Lista 2):";
    }

    if (discountNoticeEl && discountNoticeTextEl) {
      discountNoticeEl.className = "p-2.5 rounded-xl text-[11px] font-semibold border flex items-center gap-2 bg-emerald-50 border-emerald-200 text-emerald-800";
      discountNoticeTextEl.textContent = `✓ ¡Descuento aplicado por compra mayor a ${formatCurrency(minDiscount)}! Ahorro: ${formatCurrency(discountAmount)}`;
    }
    if (discountRowEl) {
      discountRowEl.classList.remove('hidden');
      if (discountLabelEl) discountLabelEl.textContent = discountLabel;
      if (discountAmountEl) discountAmountEl.textContent = `-${formatCurrency(discountAmount)}`;
    }
  } else {
    discountAmount = 0;
    const remaining = minDiscount - baseSubtotal;
    if (discountNoticeEl && discountNoticeTextEl) {
      discountNoticeEl.className = "p-2.5 rounded-xl text-[11px] font-semibold border flex items-center gap-2 bg-amber-50 border-amber-200 text-amber-800";
      if (baseSubtotal > 0) {
        discountNoticeTextEl.textContent = `Sumá ${formatCurrency(remaining)} más para acceder al Descuento Mayorista / Lista 2 (Mínimo ${formatCurrency(minDiscount)})`;
      } else {
        discountNoticeTextEl.textContent = `El descuento mayorista / especial se aplica con un monto mínimo de compra de ${formatCurrency(minDiscount)}.`;
      }
    }
    if (discountRowEl) {
      discountRowEl.classList.add('hidden');
    }
  }

  const netSubtotal = Math.max(0, Math.round((baseSubtotal - discountAmount) * 100) / 100);
  const tax = Math.round(netSubtotal * 0.21 * 100) / 100;
  const total = Math.round((netSubtotal + tax) * 100) / 100;

  // Política de Envío
  const deliveryCheck = document.getElementById('quoteDeliveryCheck');
  const isDelivery = deliveryCheck ? deliveryCheck.checked : true;
  const shippingNoticeEl = document.getElementById('quoteShippingNotice');
  const shippingNoticeTextEl = document.getElementById('quoteShippingNoticeText');
  const shippingStatusEl = document.getElementById('drawerShippingStatus');

  const prefix = commercialSettings.freeShippingLegendPrefix || "El envío es gratis a partir de ";

  if (isDelivery) {
    if (baseSubtotal >= minFreeShipping && baseSubtotal > 0) {
      if (shippingNoticeEl && shippingNoticeTextEl) {
        shippingNoticeEl.className = "p-2.5 rounded-xl text-[11px] font-semibold border flex items-center gap-2 bg-emerald-50 border-emerald-200 text-emerald-800";
        shippingNoticeTextEl.textContent = `✓ ¡Envío Gratis Bonificado! (Tu pedido supera los ${formatCurrency(minFreeShipping)})`;
      }
      if (shippingStatusEl) {
        shippingStatusEl.textContent = "Gratis";
        shippingStatusEl.className = "font-bold text-emerald-600";
      }
    } else {
      const remainingShipping = minFreeShipping - baseSubtotal;
      if (shippingNoticeEl && shippingNoticeTextEl) {
        shippingNoticeEl.className = "p-2.5 rounded-xl text-[11px] font-semibold border flex items-center gap-2 bg-sky-50 border-sky-200 text-sky-800";
        shippingNoticeTextEl.textContent = `${prefix}${formatCurrency(minFreeShipping)} (Faltan ${formatCurrency(remainingShipping)})`;
      }
      if (shippingStatusEl) {
        shippingStatusEl.textContent = "A coordinar";
        shippingStatusEl.className = "font-semibold text-slate-700";
      }
    }
  } else {
    if (shippingNoticeEl && shippingNoticeTextEl) {
      shippingNoticeEl.className = "p-2.5 rounded-xl text-[11px] font-semibold border flex items-center gap-2 bg-slate-100 border-slate-200 text-slate-700";
      shippingNoticeTextEl.textContent = "Retiro por depósito / sucursal Gasbel sin costo de envío.";
    }
    if (shippingStatusEl) {
      shippingStatusEl.textContent = "Retiro en local";
      shippingStatusEl.className = "font-semibold text-slate-700";
    }
  }

  // Update DOM Totals
  const baseSubEl = document.getElementById('drawerBaseSubtotal');
  const subEl = document.getElementById('drawerSubtotal');
  const taxEl = document.getElementById('drawerTax');
  const totEl = document.getElementById('drawerTotal');
  const footerTotEl = document.getElementById('drawerFooterTotal');

  if (baseSubEl) baseSubEl.textContent = formatCurrency(baseSubtotal);
  if (subEl) subEl.textContent = formatCurrency(netSubtotal);
  if (taxEl) taxEl.textContent = formatCurrency(tax);
  if (totEl) totEl.textContent = formatCurrency(total);
  if (footerTotEl) footerTotEl.textContent = formatCurrency(total);
}

// Generar Presupuesto formal vía API
async function submitQuote() {
  if (quoteItems.length === 0) {
    showToast('La cotización está vacía', false);
    return;
  }

  const name = (document.getElementById('quoteCustomerName')?.value || '').trim();
  const address = (document.getElementById('quoteCustomerAddress')?.value || '').trim();
  const phone = (document.getElementById('quoteCustomerPhone')?.value || '').trim();
  const email = (document.getElementById('quoteCustomerEmail')?.value || '').trim();
  const isDelivery = document.getElementById('quoteDeliveryCheck')?.checked ?? true;

  if (!name || !phone) {
    showToast('Por favor completa nombre y teléfono del cliente', false);
    return;
  }

  if (isDelivery && !address) {
    showToast('Por favor completa la dirección de entrega del cliente', false);
    document.getElementById('quoteCustomerAddress')?.focus();
    return;
  }

  const baseSubtotal = quoteItems.reduce((acc, item) => acc + item.lineSubtotal, 0);
  const wholesaleSubtotal = quoteItems.reduce((acc, item) => acc + item.lineWholesaleSubtotal, 0);
  const minDiscount = commercialSettings.minPurchaseForDiscount || 50000;
  const minFreeShipping = commercialSettings.minPurchaseForFreeShipping || 100000;

  let discountAmount = 0;
  let discountLabel = "Descuento Mayorista";
  if (baseSubtotal >= minDiscount && baseSubtotal > 0) {
    if (currentSpecialClient && currentSpecialClient.discountPercentage > 0) {
      discountAmount = Math.round(baseSubtotal * (currentSpecialClient.discountPercentage / 100) * 100) / 100;
      discountLabel = `Descuento Especial Cliente (${currentSpecialClient.discountPercentage}% OFF)`;
    } else {
      discountAmount = Math.max(0, Math.round((baseSubtotal - wholesaleSubtotal) * 100) / 100);
      discountLabel = "Descuento Mayorista (Lista 2)";
    }
  }

  const netSubtotal = Math.max(0, Math.round((baseSubtotal - discountAmount) * 100) / 100);
  const tax = Math.round(netSubtotal * 0.21 * 100) / 100;
  const total = Math.round((netSubtotal + tax) * 100) / 100;

  const shippingStatus = isDelivery 
    ? (baseSubtotal >= minFreeShipping ? "Gratis" : "A coordinar según zona")
    : "Retiro en local Gasbel";

  const payload = {
    customerId: currentSpecialClient?.id || selectedCustomerId || null,
    customerGroupId: selectedGroupId || null,
    customerName: name,
    customerPhone: phone,
    customerEmail: email,
    notes: `Dirección: ${address || 'No informada'} | Envío: ${isDelivery ? 'Sí' : 'No'} (${shippingStatus}) | ${discountLabel}: -${formatCurrency(discountAmount)}`,
    items: quoteItems.map(item => ({
      productId: item.product.id,
      quantity: item.quantity,
      packagingSelected: item.packagingSelected
    }))
  };

  try {
    const res = await fetch(`${API_BASE}/quotes`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (res.ok) {
      const quote = await res.json();
      toggleQuoteDrawer();
      openQuoteModal({
        ...quote,
        customerAddress: address,
        isDelivery: isDelivery,
        shippingStatus: shippingStatus,
        baseSubtotal: baseSubtotal,
        discountAmount: discountAmount,
        discountLabel: discountLabel,
        netSubtotal: netSubtotal,
        tax: tax,
        finalTotal: total
      });
      quoteItems = [];
      updateCartBadge();
    } else {
      showToast('Error al generar la cotización', false);
    }
  } catch (err) {
    showToast('Error de red al procesar cotización', false);
  }
}

function openQuoteModal(quote) {
  const modal = document.getElementById('quoteModal');
  document.getElementById('modalQuoteNumber').textContent = `Cotización #${(quote.id || '').substring(0, 8).toUpperCase()}`;

  const details = document.getElementById('modalQuoteDetails');
  details.innerHTML = `
    <div class="bg-white p-3 rounded-xl border border-slate-200 space-y-1 mb-2">
      <div class="flex justify-between">
        <span class="text-slate-500">Cliente:</span>
        <span class="font-bold text-slate-800">${quote.customerName} (${quote.customerPhone})</span>
      </div>
      ${quote.customerAddress ? `
        <div class="flex justify-between">
          <span class="text-slate-500">Dirección:</span>
          <span class="font-semibold text-slate-700">${quote.customerAddress}</span>
        </div>
      ` : ''}
      <div class="flex justify-between">
        <span class="text-slate-500">Modalidad de Entrega:</span>
        <span class="font-semibold text-slate-700">${quote.isDelivery ? 'Envío a domicilio' : 'Retiro en sucursal'} (${quote.shippingStatus || 'Coordinar'})</span>
      </div>
    </div>

    <div class="divide-y divide-slate-100 py-2 max-h-48 overflow-y-auto">
      ${(quote.items || []).map(it => `
        <div class="py-1 flex justify-between text-slate-700 text-xs">
          <span>${it.quantity}x ${it.productName}</span>
          <span class="font-semibold">${formatCurrency(it.lineSubtotal)}</span>
        </div>
      `).join('')}
    </div>

    <div class="pt-2 border-t border-slate-200 space-y-1 text-xs">
      <div class="flex justify-between text-slate-600">
        <span>Subtotal Lista 1 (Minorista):</span>
        <span class="font-semibold">${formatCurrency(quote.baseSubtotal || quote.subtotal)}</span>
      </div>
      ${(quote.discountAmount > 0) ? `
        <div class="flex justify-between text-emerald-600 font-bold">
          <span>${quote.discountLabel || 'Descuento Aplicado'}:</span>
          <span>-${formatCurrency(quote.discountAmount)}</span>
        </div>
      ` : ''}
      <div class="flex justify-between text-slate-600">
        <span>IVA (21%):</span>
        <span class="font-semibold">${formatCurrency(quote.tax || quote.taxAmount)}</span>
      </div>
      <div class="pt-1 border-t border-slate-200 flex justify-between font-bold text-slate-900 text-sm">
        <span>Total Presupuestado:</span>
        <span class="text-amber-600 font-black text-base">${formatCurrency(quote.finalTotal || quote.total)}</span>
      </div>
    </div>
  `;

  modal.classList.remove('hidden');
  lucide.createIcons();
}

function closeQuoteModal() {
  document.getElementById('quoteModal').classList.add('hidden');
}

// Enviar Cotización por WhatsApp con enrutamiento de número
function sendToWhatsApp() {
  if (quoteItems.length === 0) {
    showToast('No hay artículos para cotizar', false);
    return;
  }

  const name = (document.getElementById('quoteCustomerName')?.value || '').trim() || 'Cliente';
  const address = (document.getElementById('quoteCustomerAddress')?.value || '').trim();
  const phone = (document.getElementById('quoteCustomerPhone')?.value || '').trim();
  const email = (document.getElementById('quoteCustomerEmail')?.value || '').trim();
  const isDelivery = document.getElementById('quoteDeliveryCheck')?.checked ?? true;

  if (isDelivery && !address) {
    showToast('Por favor completa la dirección de entrega del cliente', false);
    document.getElementById('quoteCustomerAddress')?.focus();
    return;
  }

  const baseSubtotal = quoteItems.reduce((acc, item) => acc + item.lineSubtotal, 0);
  const wholesaleSubtotal = quoteItems.reduce((acc, item) => acc + item.lineWholesaleSubtotal, 0);
  const minDiscount = commercialSettings.minPurchaseForDiscount || 50000;
  const minFreeShipping = commercialSettings.minPurchaseForFreeShipping || 100000;

  let discountAmount = 0;
  let discountLabel = "Descuento Mayorista (Lista 2)";
  if (baseSubtotal >= minDiscount && baseSubtotal > 0) {
    if (currentSpecialClient && currentSpecialClient.discountPercentage > 0) {
      discountAmount = Math.round(baseSubtotal * (currentSpecialClient.discountPercentage / 100) * 100) / 100;
      discountLabel = `Descuento Especial (${currentSpecialClient.discountPercentage}% OFF)`;
    } else {
      discountAmount = Math.max(0, Math.round((baseSubtotal - wholesaleSubtotal) * 100) / 100);
      discountLabel = "Descuento Mayorista (Lista 2)";
    }
  }

  const netSubtotal = Math.max(0, Math.round((baseSubtotal - discountAmount) * 100) / 100);
  const tax = Math.round(netSubtotal * 0.21 * 100) / 100;
  const total = Math.round((netSubtotal + tax) * 100) / 100;

  const shippingStatus = isDelivery 
    ? (baseSubtotal >= minFreeShipping ? "Gratis (Supera compra mínima)" : "A coordinar según zona")
    : "Retiro en local Gasbel";

  let text = `*SOLICITUD DE COTIZACIÓN B2B - GASBEL*\n`;
  text += `👤 *Cliente:* ${name}\n`;
  if (phone) text += `📱 *Teléfono:* ${phone}\n`;
  if (address) text += `📍 *Dirección de entrega:* ${address}\n`;
  if (email) text += `✉️ *Email:* ${email}\n`;
  text += `🚚 *Envío solicitado:* ${isDelivery ? 'Sí' : 'No (Retiro en local)'}\n`;
  text += `📦 *Condición de envío:* ${shippingStatus}\n`;
  text += `📅 *Fecha:* ${new Date().toLocaleDateString('es-AR')}\n\n`;

  text += `*Artículos solicitados:*\n`;
  quoteItems.forEach((item, i) => {
    const units = item.effectiveUnits || item.quantity;
    text += `${i + 1}. ${item.product.name}\n`;
    if (item.product.dimensions) text += `   📏 Medida: ${item.product.dimensions}\n`;
    text += `   Cant: ${units} u. (${item.packagingSelected === 'Box' ? 'Bulto' : 'Unidad'}) | P.U. Lista 1: ${formatCurrency(item.unitPrice)} | Subt: ${formatCurrency(item.lineSubtotal)}\n\n`;
  });

  text += `------------------------------------\n`;
  text += `*Subtotal Lista 1 (Minorista):* ${formatCurrency(baseSubtotal)}\n`;
  if (discountAmount > 0) {
    text += `*${discountLabel}:* -${formatCurrency(discountAmount)}\n`;
    text += `*Subtotal con Descuento:* ${formatCurrency(netSubtotal)}\n`;
  }
  text += `*IVA (21%):* ${formatCurrency(tax)}\n`;
  text += `*TOTAL ESTIMADO:* ${formatCurrency(total)}\n`;
  text += `------------------------------------\n\n`;
  text += `_Cotización emitida desde el Catálogo B2B Gasbel._`;

  // Número de WhatsApp configurado según modo (Pruebas vs Oficial)
  const targetNumber = commercialSettings.useTestingNumber 
    ? (commercialSettings.whatsAppTesting || "2613890532") 
    : (commercialSettings.whatsAppProduction || "2616799084");

  const digits = targetNumber.replace(/\D/g, '');
  let formattedNumber = digits;
  if (!formattedNumber.startsWith('54')) {
    formattedNumber = '549' + formattedNumber;
  } else if (!formattedNumber.startsWith('549')) {
    formattedNumber = '549' + formattedNumber.substring(2);
  }

  const url = `https://wa.me/${formattedNumber}?text=${encodeURIComponent(text)}`;
  window.open(url, '_blank');
}

// ==========================================
// 6. GESTIÓN ADMIN: TABS, CLIENTES ESPECIALES & REGLAS
// ==========================================

function switchAdminTab(tabName) {
  activeAdminTab = tabName;

  const btnProducts = document.getElementById('adminTabBtn-products');
  const btnClients = document.getElementById('adminTabBtn-clients');
  const btnSettings = document.getElementById('adminTabBtn-settings');

  const tabProducts = document.getElementById('adminTab-products');
  const tabClients = document.getElementById('adminTab-clients');
  const tabSettings = document.getElementById('adminTab-settings');

  const activeBtnClass = "px-3.5 py-2 rounded-xl text-xs font-bold bg-slate-900 text-white shadow-xs flex items-center gap-2 transition";
  const inactiveBtnClass = "px-3.5 py-2 rounded-xl text-xs font-bold bg-white text-slate-700 hover:bg-slate-100 border border-slate-200 flex items-center gap-2 transition";

  if (btnProducts) btnProducts.className = tabName === 'products' ? activeBtnClass : inactiveBtnClass;
  if (btnClients) btnClients.className = tabName === 'clients' ? activeBtnClass : inactiveBtnClass;
  if (btnSettings) btnSettings.className = tabName === 'settings' ? activeBtnClass : inactiveBtnClass;

  if (tabProducts) tabProducts.classList.toggle('hidden', tabName !== 'products');
  if (tabClients) tabClients.classList.toggle('hidden', tabName !== 'clients');
  if (tabSettings) tabSettings.classList.toggle('hidden', tabName !== 'settings');

  if (tabName === 'clients') {
    renderSpecialClientsTable();
  } else if (tabName === 'settings') {
    populateCommercialSettingsForm();
  }

  lucide.createIcons();
}

// Clientes Especiales (Grilla con Porcentajes Asignados)
async function loadSpecialClients() {
  try {
    const res = await fetch(`${API_BASE}/special-clients`);
    if (res.ok) {
      specialClients = await res.json();
      updateSpecialClientsDatalist();
      renderSpecialClientsTable();
      const badge = document.getElementById('specialClientsCountBadge');
      if (badge) badge.textContent = specialClients.length;
    }
  } catch (err) {
    console.error('Error al cargar clientes especiales:', err);
  }
}

function updateSpecialClientsDatalist() {
  const datalist = document.getElementById('specialClientsDatalist');
  if (!datalist) return;
  datalist.innerHTML = '';
  specialClients.filter(c => c.isActive).forEach(c => {
    const opt = document.createElement('option');
    opt.value = c.name;
    opt.textContent = `${c.name} (${c.discountPercentage}% OFF)`;
    datalist.appendChild(opt);
  });
}

function getSpecialClientShareUrl(client) {
  const base = `${window.location.origin}${window.location.pathname}`;
  return `${base}?cliente=${encodeURIComponent(client.id)}`;
}

function copyTextToClipboard(text, successMessage) {
  if (navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(text).then(() => {
      showToast(successMessage);
    }).catch(() => {
      fallbackCopyText(text, successMessage);
    });
  } else {
    fallbackCopyText(text, successMessage);
  }
}

function fallbackCopyText(text, successMessage) {
  const input = document.createElement('textarea');
  input.value = text;
  input.style.position = 'fixed';
  input.style.opacity = '0';
  document.body.appendChild(input);
  input.focus();
  input.select();
  try {
    document.execCommand('copy');
    showToast(successMessage);
  } catch (err) {
    prompt('Copia el siguiente enlace:', text);
  }
  document.body.removeChild(input);
}

function copySpecialClientLink(clientId) {
  const c = specialClients.find(item => item.id === clientId);
  if (!c) return;

  const url = getSpecialClientShareUrl(c);
  copyTextToClipboard(url, `¡Enlace copiado! Envíale este link a ${c.name} (${c.discountPercentage}% OFF).`);
}

function shareSpecialClientWhatsApp(clientId) {
  const c = specialClients.find(item => item.id === clientId);
  if (!c) return;

  const url = getSpecialClientShareUrl(c);
  const message = `¡Hola ${c.name}! Te compartimos el catálogo oficial de Gasbel con tu beneficio exclusivo de ${c.discountPercentage}% de descuento en tus pedidos mayoristas. Podés ver tus precios e ingresar directamente desde aquí:\n${url}`;

  let targetPhone = (c.phone || '').replace(/\D/g, '');
  if (targetPhone) {
    if (!targetPhone.startsWith('54')) {
      targetPhone = '549' + targetPhone;
    } else if (!targetPhone.startsWith('549')) {
      targetPhone = '549' + targetPhone.substring(2);
    }
    window.open(`https://wa.me/${targetPhone}?text=${encodeURIComponent(message)}`, '_blank');
  } else {
    window.open(`https://wa.me/?text=${encodeURIComponent(message)}`, '_blank');
  }
}

function copyCurrentModalClientLink() {
  const input = document.getElementById('modalClientLinkInput');
  if (!input || !input.value) return;
  copyTextToClipboard(input.value, '¡Enlace del cliente copiado al portapapeles!');
}

function shareCurrentModalClientWhatsApp() {
  const id = document.getElementById('editSpecialClientId').value;
  if (!id) return;
  shareSpecialClientWhatsApp(id);
}

function renderSpecialClientsTable() {
  const tbody = document.getElementById('specialClientsTableBody');
  if (!tbody) return;
  tbody.innerHTML = '';

  if (specialClients.length === 0) {
    tbody.innerHTML = `
      <tr>
        <td colspan="8" class="text-center py-8 text-slate-400">
          No hay clientes registrados con porcentaje especial. Haz clic en "Agregar Cliente con Descuento" para crear uno.
        </td>
      </tr>
    `;
    return;
  }

  specialClients.forEach(c => {
    const tr = document.createElement('tr');
    tr.className = "hover:bg-slate-50/80 transition-colors";
    tr.innerHTML = `
      <td class="py-3 px-4 font-bold text-slate-900">
        ${c.name}
      </td>
      <td class="py-3 px-4 text-center">
        <span class="inline-flex items-center gap-1 bg-amber-100 text-amber-800 text-xs font-black px-2.5 py-0.5 rounded-full">
          <i data-lucide="percent" class="w-3 h-3"></i> ${c.discountPercentage}% OFF
        </span>
      </td>
      <td class="py-3 px-4 text-center">
        <div class="inline-flex items-center gap-1.5">
          <button onclick="copySpecialClientLink('${c.id}')" title="Copiar enlace para el cliente" class="inline-flex items-center gap-1.5 bg-amber-50 hover:bg-amber-100 active:bg-amber-200 text-amber-900 border border-amber-200/80 px-2.5 py-1.5 rounded-xl text-xs font-bold transition shadow-2xs">
            <i data-lucide="copy" class="w-3.5 h-3.5 text-amber-600"></i>
            <span>Copiar Link</span>
          </button>
          <button onclick="shareSpecialClientWhatsApp('${c.id}')" title="Enviar enlace personalizado por WhatsApp" class="p-1.5 bg-emerald-50 hover:bg-emerald-100 active:bg-emerald-200 text-emerald-700 border border-emerald-200/80 rounded-xl transition shadow-2xs">
            <i data-lucide="message-circle" class="w-4 h-4"></i>
          </button>
        </div>
      </td>
      <td class="py-3 px-4 font-mono text-slate-600">
        ${c.phone || '-'}
      </td>
      <td class="py-3 px-4 text-slate-600">
        ${c.address || '-'}
      </td>
      <td class="py-3 px-4 text-slate-500 max-w-xs truncate" title="${c.notes || ''}">
        ${c.notes || '-'}
      </td>
      <td class="py-3 px-4 text-center">
        <span class="inline-flex items-center gap-1 text-[10px] font-bold px-2 py-0.5 rounded-full ${c.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-500'}">
          ${c.isActive ? 'Activo' : 'Inactivo'}
        </span>
      </td>
      <td class="py-3 px-4 text-center">
        <div class="flex items-center justify-center gap-1.5">
          <button onclick="openSpecialClientModal('${c.id}')" title="Editar cliente" class="p-1.5 text-slate-600 hover:text-amber-600 hover:bg-slate-100 rounded-lg transition">
            <i data-lucide="edit-3" class="w-4 h-4"></i>
          </button>
          <button onclick="deleteSpecialClient('${c.id}')" title="Eliminar cliente" class="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition">
            <i data-lucide="trash-2" class="w-4 h-4"></i>
          </button>
        </div>
      </td>
    `;
    tbody.appendChild(tr);
  });

  lucide.createIcons();
}

function openSpecialClientModal(clientId = null) {
  const modal = document.getElementById('specialClientModal');
  const title = document.getElementById('specialClientModalTitle');
  const idInput = document.getElementById('editSpecialClientId');
  const nameInput = document.getElementById('editSpecialClientName');
  const discountInput = document.getElementById('editSpecialClientDiscount');
  const phoneInput = document.getElementById('editSpecialClientPhone');
  const addressInput = document.getElementById('editSpecialClientAddress');
  const notesInput = document.getElementById('editSpecialClientNotes');
  const activeInput = document.getElementById('editSpecialClientIsActive');
  const linkSection = document.getElementById('modalClientLinkSection');
  const linkInput = document.getElementById('modalClientLinkInput');

  if (clientId) {
    const c = specialClients.find(cl => cl.id === clientId);
    if (!c) return;
    title.textContent = "Editar Cliente con Descuento";
    idInput.value = c.id;
    nameInput.value = c.name;
    discountInput.value = c.discountPercentage;
    phoneInput.value = c.phone || '';
    addressInput.value = c.address || '';
    notesInput.value = c.notes || '';
    activeInput.checked = c.isActive;

    if (linkSection && linkInput) {
      linkInput.value = getSpecialClientShareUrl(c);
      linkSection.classList.remove('hidden');
    }
  } else {
    title.textContent = "Nuevo Cliente con Descuento";
    idInput.value = '';
    nameInput.value = '';
    discountInput.value = '25';
    phoneInput.value = '';
    addressInput.value = '';
    notesInput.value = '';
    activeInput.checked = true;

    if (linkSection) linkSection.classList.add('hidden');
  }

  modal.classList.remove('hidden');
  setTimeout(() => nameInput.focus(), 100);
  lucide.createIcons();
}

function closeSpecialClientModal() {
  const modal = document.getElementById('specialClientModal');
  if (modal) modal.classList.add('hidden');
}

async function saveSpecialClient(e) {
  e.preventDefault();
  const id = document.getElementById('editSpecialClientId').value;
  const name = document.getElementById('editSpecialClientName').value.trim();
  const discountPercentage = parseFloat(document.getElementById('editSpecialClientDiscount').value) || 0;
  const phone = document.getElementById('editSpecialClientPhone').value.trim();
  const address = document.getElementById('editSpecialClientAddress').value.trim();
  const notes = document.getElementById('editSpecialClientNotes').value.trim();
  const isActive = document.getElementById('editSpecialClientIsActive').checked;

  if (!name) {
    showToast('El nombre es obligatorio', false);
    return;
  }

  const payload = {
    name,
    discountPercentage,
    phone,
    address,
    notes,
    isActive
  };

  try {
    const url = id ? `${API_BASE}/admin/special-clients/${id}` : `${API_BASE}/admin/special-clients`;
    const method = id ? 'PUT' : 'POST';

    const res = await fetch(url, {
      method,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (res.ok) {
      showToast(id ? 'Cliente actualizado con éxito' : 'Cliente registrado con éxito');
      closeSpecialClientModal();
      await loadSpecialClients();
    } else {
      showToast('Error al guardar el cliente', false);
    }
  } catch (err) {
    showToast('Error de red al guardar cliente', false);
  }
}

async function deleteSpecialClient(id) {
  const client = specialClients.find(c => c.id === id);
  if (!confirm(`¿Eliminar al cliente "${client?.name || 'seleccionado'}" de la grilla de descuentos?`)) return;

  try {
    const res = await fetch(`${API_BASE}/admin/special-clients/${id}`, {
      method: 'DELETE'
    });
    if (res.ok) {
      showToast('Cliente eliminado de la grilla');
      await loadSpecialClients();
    } else {
      showToast('Error al eliminar cliente', false);
    }
  } catch (err) {
    showToast('Error de red al eliminar', false);
  }
}

// Configuración Comercial y WhatsApp
async function loadCommercialSettings() {
  try {
    const res = await fetch(`${API_BASE}/settings`);
    if (res.ok) {
      commercialSettings = await res.json();
      populateCommercialSettingsForm();
      updateDrawerTotals();
    }
  } catch (err) {
    console.error('Error al cargar configuración comercial:', err);
  }
}

function populateCommercialSettingsForm() {
  const minDiscInput = document.getElementById('settingMinPurchaseDiscount');
  const minShipInput = document.getElementById('settingMinPurchaseFreeShipping');
  const legendInput = document.getElementById('settingFreeShippingLegendPrefix');
  const prodPhoneInput = document.getElementById('settingWhatsAppProduction');
  const testPhoneInput = document.getElementById('settingWhatsAppTesting');
  const useTestCheck = document.getElementById('settingUseTestingNumber');

  if (minDiscInput) minDiscInput.value = commercialSettings.minPurchaseForDiscount;
  if (minShipInput) minShipInput.value = commercialSettings.minPurchaseForFreeShipping;
  if (legendInput) legendInput.value = commercialSettings.freeShippingLegendPrefix;
  if (prodPhoneInput) prodPhoneInput.value = commercialSettings.whatsAppProduction;
  if (testPhoneInput) testPhoneInput.value = commercialSettings.whatsAppTesting;
  if (useTestCheck) useTestCheck.checked = commercialSettings.useTestingNumber;
}

async function saveCommercialSettings(e) {
  e.preventDefault();

  const minPurchaseForDiscount = parseFloat(document.getElementById('settingMinPurchaseDiscount').value) || 50000;
  const minPurchaseForFreeShipping = parseFloat(document.getElementById('settingMinPurchaseFreeShipping').value) || 100000;
  const freeShippingLegendPrefix = document.getElementById('settingFreeShippingLegendPrefix').value || "El envío es gratis a partir de ";
  const whatsAppProduction = document.getElementById('settingWhatsAppProduction').value.trim() || "2616799084";
  const whatsAppTesting = document.getElementById('settingWhatsAppTesting').value.trim() || "2613890532";
  const useTestingNumber = document.getElementById('settingUseTestingNumber').checked;

  const payload = {
    minPurchaseForDiscount,
    minPurchaseForFreeShipping,
    freeShippingLegendPrefix,
    whatsAppProduction,
    whatsAppTesting,
    useTestingNumber
  };

  try {
    const res = await fetch(`${API_BASE}/admin/settings`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (res.ok) {
      commercialSettings = await res.json();
      showToast('Reglas comerciales actualizadas correctamente');
      updateDrawerTotals();
    } else {
      showToast('Error al guardar configuración', false);
    }
  } catch (err) {
    showToast('Error de red al guardar configuración', false);
  }
}

