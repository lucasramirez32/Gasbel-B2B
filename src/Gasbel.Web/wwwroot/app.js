// Gasbel B2B Frontend Logic
let categories = [];
let customerGroups = [];
let customers = [];
let products = [];
let selectedCategoryId = null;
let selectedGroupId = "";
let selectedCustomerId = "";
let quoteItems = []; // { product, quantity, packagingSelected, unitPrice, lineSubtotal, priceSource }
let productSelections = {}; // productId -> { quantity, packaging }

const API_BASE = '/api';

// Format currency
function formatCurrency(amount) {
  return new Intl.NumberFormat('es-AR', {
    style: 'currency',
    currency: 'ARS',
    minimumFractionDigits: 2
  }).format(amount);
}

// Init App
document.addEventListener('DOMContentLoaded', async () => {
  await Promise.all([
    loadCustomerGroups(),
    loadCustomers(),
    loadCategories()
  ]);
  await loadProducts();
  lucide.createIcons();
});

// Load Customer Groups for B2B Simulator
async function loadCustomerGroups() {
  try {
    const res = await fetch(`${API_BASE}/customer-groups`);
    if (res.ok) {
      customerGroups = await res.json();
      const select = document.getElementById('groupSelector');
      customerGroups.forEach(g => {
        const opt = document.createElement('option');
        opt.value = g.id;
        opt.textContent = `${g.name} (${g.defaultDiscountPercentage}% off)`;
        select.appendChild(opt);
      });
    }
  } catch (err) {
    console.error('Error al cargar grupos:', err);
  }
}

// Load Customers
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

// Load Categories
async function loadCategories() {
  try {
    const res = await fetch(`${API_BASE}/categories`);
    if (res.ok) {
      categories = await res.json();
      renderCategoryTabs();
    }
  } catch (err) {
    console.error('Error al cargar categorías:', err);
  }
}

function renderCategoryTabs() {
  const container = document.getElementById('categoryTabs');
  // Mantener el botón 'Todos'
  container.innerHTML = `
    <button onclick="selectCategory(null)" id="tab-all" class="cat-tab px-3.5 py-2 text-xs font-semibold rounded-lg bg-slate-900 text-white shadow-sm transition-all whitespace-nowrap">
      Todos los Productos (${categories.reduce((acc, c) => acc + c.productCount, 0)})
    </button>
  `;

  categories.forEach(c => {
    const btn = document.createElement('button');
    btn.id = `tab-${c.id}`;
    btn.className = "cat-tab px-3.5 py-2 text-xs font-semibold rounded-lg bg-white text-slate-700 hover:bg-slate-100 border border-slate-200 shadow-sm transition-all whitespace-nowrap";
    btn.textContent = `${c.name} (${c.productCount})`;
    btn.onclick = () => selectCategory(c.id);
    container.appendChild(btn);
  });
}

function selectCategory(catId) {
  selectedCategoryId = catId;
  document.querySelectorAll('.cat-tab').forEach(el => {
    el.className = "cat-tab px-3.5 py-2 text-xs font-semibold rounded-lg bg-white text-slate-700 hover:bg-slate-100 border border-slate-200 shadow-sm transition-all whitespace-nowrap";
  });

  const activeBtn = document.getElementById(catId ? `tab-${catId}` : 'tab-all');
  if (activeBtn) {
    activeBtn.className = "cat-tab px-3.5 py-2 text-xs font-semibold rounded-lg bg-slate-900 text-white shadow-sm transition-all whitespace-nowrap";
  }

  loadProducts();
}

function onGroupChanged() {
  const select = document.getElementById('groupSelector');
  selectedGroupId = select.value;

  // Actualizar banner informativo
  const banner = document.getElementById('profileBannerText');
  if (!selectedGroupId) {
    banner.textContent = "Precios minoristas estándar. Selecciona un perfil comercial para ver bonificaciones de escala.";
    selectedCustomerId = "";
  } else {
    const group = customerGroups.find(g => g.id === selectedGroupId);
    // Ver si hay un cliente representativo
    const matchingCust = customers.find(c => c.customerGroupId === selectedGroupId);
    selectedCustomerId = matchingCust ? matchingCust.id : "";
    banner.textContent = `Perfil activo: ${group.name}. Aplicando escala de descuentos para ${group.description}`;
  }

  loadProducts();
  recalculateQuoteItems();
}

let searchDebounceTimer = null;
function onSearchInput() {
  clearTimeout(searchDebounceTimer);
  searchDebounceTimer = setTimeout(() => {
    loadProducts();
  }, 250);
}

// Load products from API
async function loadProducts() {
  const grid = document.getElementById('productGrid');
  const emptyState = document.getElementById('emptyState');
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
      if (products.length === 0) {
        grid.innerHTML = '';
        emptyState.classList.remove('hidden');
      } else {
        emptyState.classList.add('hidden');
        renderProductGrid();
      }
    }
  } catch (err) {
    console.error('Error al cargar productos:', err);
  }
}

// Render product cards
function renderProductGrid() {
  const grid = document.getElementById('productGrid');
  grid.innerHTML = '';

  products.forEach(p => {
    // Inicializar selección si no existe
    if (!productSelections[p.id]) {
      productSelections[p.id] = {
        quantity: 1,
        packaging: 'Package', // 'Package' o 'Box'
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

    const card = document.createElement('div');
    card.id = `product-card-${p.id}`;
    card.className = "bg-white rounded-2xl border border-slate-200/80 shadow-sm hover:shadow-md transition-all flex flex-col justify-between overflow-hidden group";

    card.innerHTML = `
      <div class="p-5 flex-1 flex flex-col">
        <!-- Badges -->
        <div class="flex items-center justify-between gap-2 mb-2">
          <span class="text-[10px] font-bold tracking-wider uppercase px-2 py-0.5 rounded-md bg-slate-100 text-slate-600">
            ${p.sku}
          </span>
          <span class="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-amber-50 text-amber-700 border border-amber-200/60">
            ${p.categoryName}
          </span>
        </div>

        <!-- Product Name & Description -->
        <h3 class="text-sm font-bold text-slate-900 group-hover:text-amber-600 transition-colors line-clamp-2 mb-1.5">
          ${p.name}
        </h3>
        <p class="text-xs text-slate-500 line-clamp-2 mb-4 leading-relaxed">
          ${p.description}
        </p>

        <!-- Packaging Indicator -->
        <div class="mt-auto bg-slate-50 rounded-xl p-2.5 mb-4 border border-slate-100 text-xs space-y-1">
          <div class="flex items-center justify-between text-slate-600">
            <span>Presentación:</span>
            <span class="font-semibold text-slate-800">${p.unitOfMeasure}</span>
          </div>
          <div class="flex items-center justify-between text-slate-600">
            <span>Bulto Cerrado:</span>
            <span class="font-semibold text-slate-800">${p.unitsPerBox} ${p.unitOfMeasure}s</span>
          </div>
        </div>

        <!-- Live Price & Savings Tag -->
        <div class="flex items-baseline justify-between mb-3">
          <div>
            <div class="flex items-baseline gap-1.5">
              <span id="price-display-${p.id}" class="text-lg font-extrabold text-slate-900">
                ${formatCurrency(sel.calculatedPrice)}
              </span>
              <span class="text-[11px] text-slate-400 font-medium">/ ${p.unitOfMeasure}</span>
            </div>
            ${p.basePrice > sel.calculatedPrice ? `
              <div class="flex items-center gap-1.5 mt-0.5">
                <span class="text-xs text-slate-400 line-through">${formatCurrency(p.basePrice)}</span>
                <span id="savings-badge-${p.id}" class="text-[10px] font-bold bg-emerald-50 text-emerald-700 px-1.5 py-0.2 rounded border border-emerald-200">
                  ${sel.savingsPercentage}% OFF
                </span>
              </div>
            ` : `
              <div id="savings-badge-wrapper-${p.id}"></div>
            `}
          </div>

          <span id="source-badge-${p.id}" class="text-[10px] font-medium px-2 py-0.5 rounded-full ${getSourceBadgeClass(sel.priceSource)}">
            ${getSourceBadgeLabel(sel.priceSource)}
          </span>
        </div>

        <!-- Scale Opportunity Prompt -->
        <div id="tier-hint-${p.id}" class="text-[11px] min-h-[28px] mb-3 p-1.5 rounded-lg flex items-center gap-1.5 ${getTierHintClass(sel)}">
          ${renderTierHintText(p, sel)}
        </div>

        <!-- Packaging Toggle (Paquete vs Bulto) -->
        <div class="grid grid-cols-2 gap-1 bg-slate-100 p-1 rounded-xl mb-3">
          <button 
            type="button" 
            onclick="setProductPackaging('${p.id}', 'Package')"
            id="pkg-btn-pkg-${p.id}"
            class="text-xs font-semibold py-1.5 rounded-lg transition-all ${!isBox ? 'bg-white shadow text-slate-800' : 'text-slate-500 hover:text-slate-800'}">
            Por Paquete
          </button>
          <button 
            type="button" 
            onclick="setProductPackaging('${p.id}', 'Box')"
            id="pkg-btn-box-${p.id}"
            class="text-xs font-semibold py-1.5 rounded-lg transition-all ${isBox ? 'bg-amber-500 shadow text-white' : 'text-slate-500 hover:text-slate-800'}">
            Bulto (${p.unitsPerBox} u)
          </button>
        </div>

        <!-- Quantity & Subtotal Row -->
        <div class="flex items-center justify-between gap-3 pt-2 border-t border-slate-100">
          <div class="flex items-center border border-slate-200 rounded-lg overflow-hidden bg-white">
            <button onclick="adjustQuantity('${p.id}', -1)" class="px-2.5 py-1 text-slate-600 hover:bg-slate-100 text-sm font-bold">-</button>
            <input 
              type="number" 
              id="qty-input-${p.id}" 
              min="1" 
              value="${sel.quantity}" 
              onchange="onQuantityInput('${p.id}', this.value)"
              class="w-12 text-center text-xs font-bold text-slate-800 border-0 outline-none p-1"
            />
            <button onclick="adjustQuantity('${p.id}', 1)" class="px-2.5 py-1 text-slate-600 hover:bg-slate-100 text-sm font-bold">+</button>
          </div>

          <div class="text-right">
            <div class="text-[10px] text-slate-400 font-medium">Subtotal</div>
            <div id="subtotal-${p.id}" class="text-xs font-bold text-slate-900">
              ${formatCurrency(sel.calculatedPrice * effectiveUnits)}
            </div>
          </div>
        </div>
      </div>

      <!-- Add to Quote Button -->
      <div class="p-4 bg-slate-50 border-t border-slate-100">
        <button 
          onclick="addToQuote('${p.id}')"
          class="w-full bg-slate-900 hover:bg-amber-600 active:bg-amber-700 text-white font-semibold text-xs py-2 rounded-xl transition-all flex items-center justify-center gap-1.5 shadow-sm">
          <i data-lucide="plus" class="w-3.5 h-3.5"></i> Agregar a Cotización
        </button>
      </div>
    `;

    grid.appendChild(card);
  });

  lucide.createIcons();
}

function getSourceBadgeClass(source) {
  switch (source) {
    case 'TierPrice': return 'bg-purple-100 text-purple-700 font-bold';
    case 'CustomerPrice': return 'bg-emerald-100 text-emerald-700 font-bold';
    case 'GroupPrice': return 'bg-blue-100 text-blue-700 font-bold';
    default: return 'bg-slate-100 text-slate-600';
  }
}

function getSourceBadgeLabel(source) {
  switch (source) {
    case 'TierPrice': return 'Escala Volumen';
    case 'CustomerPrice': return 'Acuerdo Especial';
    case 'GroupPrice': return 'Precio Grupo';
    default: return 'Precio Lista';
  }
}

function getTierHintClass(sel) {
  if (sel.priceSource === 'TierPrice') {
    return 'bg-purple-50 text-purple-900 border border-purple-200/60 font-medium';
  }
  return 'bg-amber-50/70 text-amber-900 border border-amber-200/40';
}

function renderTierHintText(product, sel) {
  const isBox = sel.packaging === 'Box';
  const effectiveUnits = isBox ? sel.quantity * product.UnitsPerBox : sel.quantity;

  if (sel.priceSource === 'TierPrice') {
    return `<i data-lucide="check-check" class="w-3.5 h-3.5 text-purple-600"></i> ¡Precio mayorista por bulto/volumen activado!`;
  }

  // Si tiene tiers futuros
  if (product.tiers && product.tiers.length > 0) {
    const nextTier = product.tiers.find(t => t.minQuantity > effectiveUnits);
    if (nextTier) {
      const diff = nextTier.minQuantity - effectiveUnits;
      return `<i data-lucide="sparkles" class="w-3.5 h-3.5 text-amber-600"></i> Agrega ${diff} u. más para pagar ${formatCurrency(nextTier.unitPrice)}/u`;
    }
  }

  return `<i data-lucide="box" class="w-3.5 h-3.5 text-slate-400"></i> Bulto cerrado de ${product.unitsPerBox} unidades`;
}

// Adjust Quantity
async function adjustQuantity(productId, delta) {
  const sel = productSelections[productId];
  if (!sel) return;
  sel.quantity = Math.max(1, sel.quantity + delta);
  document.getElementById(`qty-input-${productId}`).value = sel.quantity;
  await calculateCardPricing(productId);
}

async function onQuantityInput(productId, val) {
  const sel = productSelections[productId];
  if (!sel) return;
  const num = parseInt(val, 10);
  sel.quantity = isNaN(num) || num < 1 ? 1 : num;
  await calculateCardPricing(productId);
}

// Toggle Packaging (Package vs Box)
async function setProductPackaging(productId, packaging) {
  const sel = productSelections[productId];
  if (!sel) return;
  sel.packaging = packaging;

  const pkgBtn = document.getElementById(`pkg-btn-pkg-${productId}`);
  const boxBtn = document.getElementById(`pkg-btn-box-${productId}`);

  if (packaging === 'Box') {
    pkgBtn.className = "text-xs font-semibold py-1.5 rounded-lg transition-all text-slate-500 hover:text-slate-800";
    boxBtn.className = "text-xs font-semibold py-1.5 rounded-lg transition-all bg-amber-500 shadow text-white";
  } else {
    pkgBtn.className = "text-xs font-semibold py-1.5 rounded-lg transition-all bg-white shadow text-slate-800";
    boxBtn.className = "text-xs font-semibold py-1.5 rounded-lg transition-all text-slate-500 hover:text-slate-800";
  }

  await calculateCardPricing(productId);
}

// Calculate pricing dynamically via backend
async function calculateCardPricing(productId) {
  const p = products.find(item => item.id === productId);
  const sel = productSelections[productId];
  if (!p || !sel) return;

  const isBox = sel.packaging === 'Box';
  const effectiveUnits = isBox ? sel.quantity * p.unitsPerBox : sel.quantity;

  try {
    const res = await fetch(`${API_BASE}/pricing/calculate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        productId: productId,
        quantity: effectiveUnits,
        customerId: selectedCustomerId || null,
        customerGroupId: selectedGroupId || null
      })
    });

    if (res.ok) {
      const data = await res.json();
      sel.calculatedPrice = data.unitPrice;
      sel.priceSource = data.appliedSource;
      sel.savingsPercentage = data.savingsPercentage;

      // Actualizar UI
      document.getElementById(`price-display-${productId}`).textContent = formatCurrency(data.unitPrice);
      document.getElementById(`subtotal-${productId}`).textContent = formatCurrency(data.subtotal);

      // Badge de fuente
      const sourceBadge = document.getElementById(`source-badge-${productId}`);
      sourceBadge.className = `text-[10px] font-medium px-2 py-0.5 rounded-full ${getSourceBadgeClass(data.appliedSource)}`;
      sourceBadge.textContent = getSourceBadgeLabel(data.appliedSource);

      // Badge de ahorro
      const badgeWrap = document.getElementById(`savings-badge-wrapper-${productId}`);
      if (p.basePrice > data.unitPrice) {
        badgeWrap.innerHTML = `
          <div class="flex items-center gap-1.5 mt-0.5">
            <span class="text-xs text-slate-400 line-through">${formatCurrency(p.basePrice)}</span>
            <span class="text-[10px] font-bold bg-emerald-50 text-emerald-700 px-1.5 py-0.2 rounded border border-emerald-200">
              ${data.savingsPercentage}% OFF
            </span>
          </div>
        `;
      } else {
        badgeWrap.innerHTML = '';
      }

      // Hint de escala
      const hint = document.getElementById(`tier-hint-${productId}`);
      hint.className = `text-[11px] min-h-[28px] mb-3 p-1.5 rounded-lg flex items-center gap-1.5 ${getTierHintClass(sel)}`;
      if (data.appliedSource === 'TierPrice') {
        hint.innerHTML = `<i data-lucide="check-check" class="w-3.5 h-3.5 text-purple-600"></i> ¡Precio mayorista por escala activado!`;
      } else if (data.unitsNeededForNextTier) {
        hint.innerHTML = `<i data-lucide="sparkles" class="w-3.5 h-3.5 text-amber-600"></i> Agrega ${data.unitsNeededForNextTier} u. más para pagar ${formatCurrency(data.nextTierUnitPrice)}/u`;
      } else {
        hint.innerHTML = `<i data-lucide="box" class="w-3.5 h-3.5 text-slate-400"></i> Bulto cerrado de ${p.unitsPerBox} unidades`;
      }

      lucide.createIcons();
    }
  } catch (err) {
    console.error('Error calculando precio:', err);
  }
}

// Add item to quote cart
function addToQuote(productId) {
  const p = products.find(item => item.id === productId);
  const sel = productSelections[productId];
  if (!p || !sel) return;

  const existing = quoteItems.find(i => i.productId === productId && i.packagingSelected === sel.packaging);
  if (existing) {
    existing.quantity += sel.quantity;
  } else {
    quoteItems.push({
      productId: p.id,
      product: p,
      quantity: sel.quantity,
      packagingSelected: sel.packaging,
      unitPrice: sel.calculatedPrice,
      priceSource: sel.priceSource,
      lineSubtotal: sel.calculatedPrice * (sel.packaging === 'Box' ? sel.quantity * p.unitsPerBox : sel.quantity)
    });
  }

  updateQuoteDrawer();
  toggleQuoteDrawer(true);
}

// Recalculate quote items when profile changes
async function recalculateQuoteItems() {
  for (const item of quoteItems) {
    const isBox = item.packagingSelected === 'Box';
    const effectiveUnits = isBox ? item.quantity * item.product.unitsPerBox : item.quantity;

    try {
      const res = await fetch(`${API_BASE}/pricing/calculate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          productId: item.productId,
          quantity: effectiveUnits,
          customerId: selectedCustomerId || null,
          customerGroupId: selectedGroupId || null
        })
      });
      if (res.ok) {
        const data = await res.json();
        item.unitPrice = data.unitPrice;
        item.lineSubtotal = data.subtotal;
        item.priceSource = data.appliedSource;
      }
    } catch (e) {
      console.error(e);
    }
  }
  updateQuoteDrawer();
}

function toggleQuoteDrawer(forceOpen = null) {
  const drawer = document.getElementById('quoteDrawer');
  if (forceOpen === true) {
    drawer.classList.remove('hidden');
  } else if (forceOpen === false) {
    drawer.classList.add('hidden');
  } else {
    drawer.classList.toggle('hidden');
  }
  lucide.createIcons();
}

function updateQuoteDrawer() {
  const badge = document.getElementById('cartCountBadge');
  badge.textContent = quoteItems.reduce((acc, i) => acc + i.quantity, 0);

  const container = document.getElementById('quoteItemsList');
  if (quoteItems.length === 0) {
    container.innerHTML = `
      <div class="text-center py-12 text-slate-400">
        <i data-lucide="shopping-bag" class="w-10 h-10 mx-auto mb-2 opacity-40"></i>
        <p class="text-xs">No hay productos en la cotización</p>
      </div>
    `;
    document.getElementById('drawerSubtotal').textContent = formatCurrency(0);
    document.getElementById('drawerTax').textContent = formatCurrency(0);
    document.getElementById('drawerTotal').textContent = formatCurrency(0);
    lucide.createIcons();
    return;
  }

  container.innerHTML = '';
  let subtotal = 0;

  quoteItems.forEach((item, index) => {
    const isBox = item.packagingSelected === 'Box';
    const effectiveUnits = isBox ? item.quantity * item.product.unitsPerBox : item.quantity;
    const lineTotal = item.unitPrice * effectiveUnits;
    subtotal += lineTotal;

    const row = document.createElement('div');
    row.className = "py-3 flex items-center justify-between gap-3 text-xs";
    row.innerHTML = `
      <div class="flex-1">
        <div class="font-bold text-slate-900">${item.product.name}</div>
        <div class="text-[11px] text-slate-500 mt-0.5">
          ${item.quantity} ${isBox ? `bulto(s) de ${item.product.unitsPerBox} u.` : `paquete(s)`} × ${formatCurrency(item.unitPrice)}
        </div>
        <span class="inline-block mt-1 text-[9px] font-bold px-1.5 py-0.2 rounded-full ${getSourceBadgeClass(item.priceSource)}">
          ${getSourceBadgeLabel(item.priceSource)}
        </span>
      </div>

      <div class="text-right flex items-center gap-2">
        <span class="font-bold text-slate-900">${formatCurrency(lineTotal)}</span>
        <button onclick="removeQuoteItem(${index})" class="text-slate-400 hover:text-red-600 p-1 rounded">
          <i data-lucide="trash-2" class="w-3.5 h-3.5"></i>
        </button>
      </div>
    `;
    container.appendChild(row);
  });

  const tax = subtotal * 0.21;
  const total = subtotal + tax;

  document.getElementById('drawerSubtotal').textContent = formatCurrency(subtotal);
  document.getElementById('drawerTax').textContent = formatCurrency(tax);
  document.getElementById('drawerTotal').textContent = formatCurrency(total);
  lucide.createIcons();
}

function removeQuoteItem(index) {
  quoteItems.splice(index, 1);
  updateQuoteDrawer();
}

// Submit formal quote to backend
async function submitQuote() {
  if (quoteItems.length === 0) {
    alert("Agrega al menos un artículo a la cotización.");
    return;
  }

  const name = document.getElementById('quoteCustomerName').value.trim();
  const phone = document.getElementById('quoteCustomerPhone').value.trim();
  const email = document.getElementById('quoteCustomerEmail').value.trim();

  if (!name || !phone) {
    alert("Por favor ingresa tu Nombre / Razón Social y Teléfono.");
    return;
  }

  const btn = document.getElementById('btnSubmitQuote');
  btn.disabled = true;
  btn.innerHTML = `<i data-lucide="loader-2" class="w-4 h-4 animate-spin"></i> Generando...`;
  lucide.createIcons();

  const payload = {
    customerId: selectedCustomerId || null,
    customerGroupId: selectedGroupId || null,
    customerName: name,
    customerPhone: phone,
    customerEmail: email,
    notes: `Cotización generada desde catálogo B2B (${selectedGroupId ? 'Grupo Comercial' : 'Minorista'})`,
    items: quoteItems.map(i => ({
      productId: i.productId,
      quantity: i.quantity,
      packagingSelected: i.packagingSelected
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
      showQuoteModal(quote);
      quoteItems = [];
      updateQuoteDrawer();
      toggleQuoteDrawer(false);
    } else {
      alert("Ocurrió un error al generar la cotización.");
    }
  } catch (err) {
    console.error(err);
    alert("Error de conexión al servidor.");
  } finally {
    btn.disabled = false;
    btn.innerHTML = `<i data-lucide="check-circle" class="w-4 h-4"></i> Generar Presupuesto`;
    lucide.createIcons();
  }
}

// Show success modal
function showQuoteModal(quote) {
  const modal = document.getElementById('quoteModal');
  document.getElementById('modalQuoteNumber').textContent = `Cotización #${quote.id.substring(0, 8).toUpperCase()}`;

  const details = document.getElementById('modalQuoteDetails');
  details.innerHTML = `
    <div class="flex justify-between border-b pb-1.5 font-semibold text-slate-800">
      <span>Cliente:</span>
      <span>${quote.customerName} (${quote.customerPhone})</span>
    </div>
    <div class="flex justify-between text-slate-600">
      <span>Fecha:</span>
      <span>${new Date(quote.createdAt).toLocaleString('es-AR')}</span>
    </div>
    <div class="pt-2 divide-y divide-slate-100">
      ${quote.items.map(i => `
        <div class="py-1 flex justify-between">
          <span>${i.productName} (${i.quantity} u.)</span>
          <span class="font-semibold text-slate-900">${formatCurrency(i.lineSubtotal)}</span>
        </div>
      `).join('')}
    </div>
    <div class="pt-2 border-t border-slate-200 space-y-1 font-medium">
      <div class="flex justify-between text-slate-600">
        <span>Subtotal Neto:</span>
        <span>${formatCurrency(quote.subtotal)}</span>
      </div>
      <div class="flex justify-between text-slate-600">
        <span>IVA (21%):</span>
        <span>${formatCurrency(quote.taxAmount)}</span>
      </div>
      <div class="flex justify-between text-sm font-bold text-amber-600 pt-1 border-t">
        <span>Total Presupuestado:</span>
        <span>${formatCurrency(quote.total)}</span>
      </div>
    </div>
  `;

  modal.classList.remove('hidden');
  lucide.createIcons();
}

function closeQuoteModal() {
  document.getElementById('quoteModal').classList.add('hidden');
}

// Send quote via WhatsApp
function sendToWhatsApp() {
  if (quoteItems.length === 0) {
    alert("Agrega al menos un artículo para enviar la cotización.");
    return;
  }

  const name = document.getElementById('quoteCustomerName').value.trim() || "Cliente Gasbel";
  let message = `Hola Gasbel! Quisiera solicitar cotización formal de los siguientes descartables:\n\n*Cliente:* ${name}\n\n*Detalle del Pedido:*\n`;

  let subtotal = 0;
  quoteItems.forEach((i, idx) => {
    const isBox = i.packagingSelected === 'Box';
    const effectiveUnits = isBox ? i.quantity * i.product.unitsPerBox : i.quantity;
    const line = i.unitPrice * effectiveUnits;
    subtotal += line;
    message += `${idx + 1}. ${i.product.name} - ${i.quantity} ${isBox ? 'Bultos' : 'Paquetes'} (${effectiveUnits} u.) a ${formatCurrency(i.unitPrice)}/u => ${formatCurrency(line)}\n`;
  });

  const tax = subtotal * 0.21;
  const total = subtotal + tax;

  message += `\n*Subtotal:* ${formatCurrency(subtotal)}`;
  message += `\n*IVA (21%):* ${formatCurrency(tax)}`;
  message += `\n*Total Estimado:* ${formatCurrency(total)}\n\nPor favor confirmar disponibilidad y plazo de entrega. Gracias!`;

  const encoded = encodeURIComponent(message);
  // Número de ventas de ejemplo de Gasbel
  window.open(`https://wa.me/5491144558899?text=${encoded}`, '_blank');
}
