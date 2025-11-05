document.addEventListener('DOMContentLoaded', () => {
    // =========================================
    // 1. VARIABLES GLOBALES Y REFERENCIAS
    // =========================================
    const API_URL = '/api';
    let bootstrapModal; // Para manejar el modal con Bootstrap

    // Referencias de navegación
    const navLinks = {
        'nav-home': 'vista-home',
        'nav-admin-envios': 'vista-admin-envios',
        'nav-admin-clientes': 'vista-admin-clientes',
        'nav-registrar-envio': 'vista-registrar-envio'
    };

    // Referencias globales
    const mensajeGlobal = document.getElementById('mensaje-global');

    // =========================================
    // 2. SISTEMA DE NAVEGACIÓN (ROUTING SIMPLE)
    // =========================================
    function mostrarVista(vistaId) {
        // Ocultar todas las vistas
        document.querySelectorAll('.vista').forEach(v => v.classList.remove('vista-activa'));
        // Mostrar la deseada
        document.getElementById(vistaId).classList.add('vista-activa');

        // Acciones específicas al entrar a una vista
        if (vistaId === 'vista-admin-clientes') cargarListaClientesAdmin();
        if (vistaId === 'vista-admin-envios') cargarClientesFiltro();
        if (vistaId === 'vista-registrar-envio') {
            cargarClientesCombo();
            cargarEstados();
        }
    }

    // Asignar eventos click al menú
    Object.keys(navLinks).forEach(navId => {
        document.getElementById(navId).addEventListener('click', (e) => {
            e.preventDefault();
            mostrarVista(navLinks[navId]);
        });
    });

    // =========================================
    // 3. FUNCIONES COMPARTIDAS
    // =========================================
    function mostrarMensaje(msg, tipo = 'success') {
        mensajeGlobal.className = `alert alert-${tipo} fixed-top container mt-3`;
        mensajeGlobal.textContent = (tipo === 'success' ? '✅ ' : '❌ ') + msg;
        mensajeGlobal.classList.remove('d-none');
        setTimeout(() => mensajeGlobal.classList.add('d-none'), 4000);
    }

    async function fetchAPI(endpoint, options = {}) {
        try {
            const response = await fetch(`${API_URL}${endpoint}`, options);
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                throw new Error(errorData.mensaje || `Error ${response.status}`);
            }
            return await response.json();
        } catch (error) {
            mostrarMensaje(error.message, 'danger');
            throw error;
        }
    }

    // =========================================
    // 4. LÓGICA: ADMIN CLIENTES
    // =========================================
    async function cargarListaClientesAdmin() {
        const lista = document.getElementById('lista-clientes-admin');
        lista.innerHTML = '<li class="list-group-item">Cargando...</li>';
        try {
            const clientes = await fetchAPI('/clientes');
            lista.innerHTML = '';
            clientes.forEach(c => {
                lista.innerHTML += `
                    <li class="list-group-item d-flex justify-content-between align-items-center">
                        <div><strong>${c.nombre}</strong> <br><small class="text-muted">${c.correo}</small></div>
                        <button class="btn btn-outline-danger btn-sm btn-eliminar-cliente" data-id="${c.id_cliente}">Eliminar</button>
                    </li>`;
            });
            // Asignar eventos a los botones de eliminar recién creados
            document.querySelectorAll('.btn-eliminar-cliente').forEach(btn => {
                btn.addEventListener('click', () => eliminarCliente(btn.dataset.id));
            });
        } catch (e) { lista.innerHTML = '<li class="list-group-item text-danger">Error al cargar clientes</li>'; }
    }

    async function registrarCliente(e) {
        e.preventDefault();
        const nombre = document.getElementById('cliente-nombre').value.trim();
        const correo = document.getElementById('cliente-correo').value.trim();
        try {
            const res = await fetchAPI('/clientes', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ nombre, correo })
            });
            mostrarMensaje(res.mensaje);
            document.getElementById('form-cliente').reset();
            cargarListaClientesAdmin();
        } catch (e) { /* Error ya manejado en fetchAPI */ }
    }

    async function eliminarCliente(id) {
        if (!confirm('¿Seguro que desea eliminar este cliente?')) return;
        try {
            const res = await fetchAPI(`/clientes/${id}`, { method: 'DELETE' });
            mostrarMensaje(res.mensaje);
            cargarListaClientesAdmin();
        } catch (e) { /* Error ya manejado */ }
    }

    // =========================================
    // 5. LÓGICA: ADMIN ENVÍOS
    // =========================================
    async function cargarClientesFiltro() {
        const select = document.getElementById('filtro-cliente-envios');
        select.innerHTML = '<option value="">Cargando...</option>';
        try {
            const clientes = await fetchAPI('/clientes');
            select.innerHTML = '<option value="">Seleccione un cliente...</option>';
            clientes.forEach(c => select.innerHTML += `<option value="${c.id_cliente}">${c.nombre}</option>`);
        } catch (e) { select.innerHTML = '<option>Error al cargar</option>'; }
    }

    async function cargarEnviosDeCliente(idCliente) {
        const tbody = document.getElementById('tabla-envios-body');
        const container = document.getElementById('tabla-envios-container');

        if (!idCliente) {
            container.classList.add('d-none');
            return;
        }

        tbody.innerHTML = '<tr><td colspan="6" class="text-center">Cargando...</td></tr>';
        container.classList.remove('d-none');

        try {
            const envios = await fetchAPI(`/clientes/${idCliente}/envios`);
            if (envios.length === 0) {
                tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">Este cliente no tiene envíos</td></tr>';
                return;
            }
            tbody.innerHTML = '';
            envios.forEach(e => {
                // Creamos el select de estatus para cada fila
                const estatusOptions = ['Registrado', 'En preparación', 'Enviado', 'Entregado']
                    .map(est => `<option value="${est}" ${e.estatus === est ? 'selected' : ''}>${est}</option>`)
                    .join('');

                tbody.innerHTML += `
                    <tr>
                        <td>#${e.id_envio}</td>
                        <td>${e.descripcion}</td>
                        <td>${e.ciudad_destino}</td>
                        <td>${new Date(e.fecha_envio).toLocaleDateString()}</td>
                        <td>
                            <select class="form-select form-select-sm select-estatus" data-id="${e.id_envio}" style="width:auto;">
                                ${estatusOptions}
                            </select>
                        </td>
                        <td>
                            <button class="btn btn-sm btn-info text-white btn-ver-detalle" data-id="${e.id_envio}">Ver Detalle</button>
                        </td>
                    </tr>`;
            });

            // Eventos para cambio de estatus y ver detalle
            document.querySelectorAll('.select-estatus').forEach(sel => {
                sel.addEventListener('change', (e) => actualizarEstatus(e.target.dataset.id, e.target.value));
            });
            document.querySelectorAll('.btn-ver-detalle').forEach(btn => {
                btn.addEventListener('click', () => verDetalleEnvio(btn.dataset.id));
            });

        } catch (e) { tbody.innerHTML = '<tr><td colspan="6" class="text-danger">Error al cargar datos</td></tr>'; }
    }

    async function actualizarEstatus(idEnvio, nuevoEstatus) {
        try {
            const res = await fetchAPI(`/envios/${idEnvio}/estatus`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ estatus: nuevoEstatus })
            });
            mostrarMensaje(`Envío #${idEnvio}: ${res.mensaje}`);
        } catch (e) { /* Error ya manejado */ }
    }

    // =========================================
    // 6. LÓGICA: REGISTRAR ENVÍO (Existente)
    // =========================================
    async function cargarClientesCombo() {
        const select = document.getElementById('cliente');
        try {
            const clientes = await fetchAPI('/clientes');
            select.innerHTML = '<option value="">Seleccione un cliente...</option>';
            clientes.forEach(c => select.innerHTML += `<option value="${c.id_cliente}">${c.nombre}</option>`);
        } catch (e) { }
    }
    async function cargarEstados() {
        const select = document.getElementById('estado');
        try {
            const estados = await fetchAPI('/estados');
            estados.forEach(e => select.innerHTML += `<option value="${e.id_estado}">${e.nombre_estado}</option>`);
        } catch (e) { }
    }
    async function cargarCiudades(idEstado) {
        const selectCiudad = document.getElementById('ciudad');
        selectCiudad.innerHTML = '<option>Cargando...</option>'; selectCiudad.disabled = true;
        try {
            const ciudades = await fetchAPI(`/ciudades/${idEstado}`);
            selectCiudad.innerHTML = '<option value="">Seleccione ciudad...</option>';
            ciudades.forEach(c => selectCiudad.innerHTML += `<option value="${c.id_ciudad}">${c.nombre_ciudad}</option>`);
            selectCiudad.disabled = false;
        } catch (e) { selectCiudad.innerHTML = '<option>Error al cargar</option>'; }
    }
    async function registrarNuevoEnvio(e) {
        e.preventDefault();
        try {
            const res = await fetchAPI('/envios', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    id_cliente: document.getElementById('cliente').value,
                    id_ciudad: document.getElementById('ciudad').value,
                    descripcion: document.getElementById('descripcion').value
                })
            });
            mostrarMensaje(res.mensaje);
            e.target.reset();
            document.getElementById('ciudad').innerHTML = '<option>Seleccione estado primero...</option>';
            document.getElementById('ciudad').disabled = true;
        } catch (e) { /* Error ya manejado */ }
    }

    // =========================================
    // 7. MODAL DETALLE
    // =========================================
    async function verDetalleEnvio(idEnvio) {
        const modalBody = document.getElementById('modal-contenido-cuerpo');
        modalBody.innerHTML = '<div class="text-center">Cargando datos...</div>';
        bootstrapModal = new bootstrap.Modal(document.getElementById('modalDetalle'));
        bootstrapModal.show();

        try {
            const d = await fetchAPI(`/envios/${idEnvio}`);
            modalBody.innerHTML = `
                <div class="row">
                    <div class="col-md-6">
                        <h6>📦 Información del Paquete</h6>
                        <p><strong>ID:</strong> #${d.id_envio}<br>
                        <strong>Descripción:</strong> ${d.descripcion}<br>
                        <strong>Estatus:</strong> <span class="badge bg-primary">${d.estatus}</span><br>
                        <strong>Fecha:</strong> ${new Date(d.fecha_envio).toLocaleString()}</p>
                    </div>
                    <div class="col-md-6">
                        <h6>📍 Destino y Cliente</h6>
                        <p><strong>Cliente:</strong> ${d.nombre_cliente} (${d.correo})<br>
                        <strong>Ciudad:</strong> ${d.ciudad_destino}<br>
                        <strong>Estado:</strong> ${d.estado_destino}</p>
                    </div>
                </div>`;
        } catch (e) { modalBody.innerHTML = '<p class="text-danger">Error al cargar detalle</p>'; }
    }

    // =========================================
    // 8. INICIALIZACIÓN DE EVENTOS
    // =========================================
    document.getElementById('filtro-cliente-envios').addEventListener('change', (e) => cargarEnviosDeCliente(e.target.value));
    document.getElementById('form-cliente').addEventListener('submit', registrarCliente);
    document.getElementById('estado').addEventListener('change', (e) => cargarCiudades(e.target.value));
    document.getElementById('form-envio').addEventListener('submit', registrarNuevoEnvio);

    // Iniciar en la portada
    mostrarVista('vista-home');
});