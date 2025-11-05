// public/app.js

// Espera a que el DOM esté completamente cargado
document.addEventListener('DOMContentLoaded', () => {

    // --- 1. Referencias a elementos del DOM ---
    const form = document.getElementById('form-envio');
    const selectEstado = document.getElementById('estado');
    const selectCiudad = document.getElementById('ciudad');
    const selectCliente = document.getElementById('cliente');
    const inputDescripcion = document.getElementById('descripcion');
    const mensajeArea = document.getElementById('mensaje-area');
    const btnRegistrar = document.getElementById('btn-registrar');

    // Nuevas referencias
    const formCliente = document.getElementById('form-cliente');
    const inputClienteNombre = document.getElementById('cliente-nombre');
    const inputClienteCorreo = document.getElementById('cliente-correo');
    const btnVerEnvios = document.getElementById('btn-ver-envios');
    const btnEliminarCliente = document.getElementById('btn-eliminar-cliente');
    const enviosArea = document.getElementById('envios-area');

    // Referencias del Modal
    const modalDetalle = document.getElementById('modal-detalle');
    const modalContenido = document.getElementById('modal-contenido-cuerpo');
    const modalCerrar = document.getElementById('modal-cerrar');

    // URL base de tu API
    const API_URL = '/api';

    // --- 2. Función para cargar Estados ---
    async function cargarEstados() {
        try {
            const response = await fetch(`${API_URL}/estados`);
            if (!response.ok) throw new Error('No se pudieron cargar los estados');
            const estados = await response.json();
            estados.forEach(estado => {
                const option = document.createElement('option');
                option.value = estado.id_estado;
                option.textContent = estado.nombre_estado;
                selectEstado.appendChild(option);
            });
        } catch (error) {
            mostrarMensaje('Error al cargar estados: ' + error.message, 'danger');
        }
    }

    // --- 3. Función para cargar Clientes ---
    async function cargarClientes() {
        selectCliente.innerHTML = '<option value="">Seleccione un cliente...</option>';
        try {
            const response = await fetch(`${API_URL}/clientes`);
            if (!response.ok) throw new Error('No se pudieron cargar los clientes');
            const clientes = await response.json();
            clientes.forEach(cliente => {
                const option = document.createElement('option');
                option.value = cliente.id_cliente;
                option.textContent = cliente.nombre;
                selectCliente.appendChild(option);
            });
        } catch (error) {
            mostrarMensaje('Error al cargar clientes: ' + error.message, 'danger');
        }
    }

    // --- 4. Función para cargar Ciudades ---
    async function cargarCiudades(idEstado) {
        selectCiudad.innerHTML = '<option value="">Cargando...</option>';
        selectCiudad.disabled = true;
        if (!idEstado) {
            selectCiudad.innerHTML = '<option value="">Seleccione un estado primero...</option>';
            return;
        }
        try {
            const response = await fetch(`${API_URL}/ciudades/${idEstado}`);
            if (!response.ok) throw new Error('No se pudieron cargar las ciudades');
            const ciudades = await response.json();
            selectCiudad.innerHTML = '<option value="">Seleccione una ciudad...</option>';
            ciudades.forEach(ciudad => {
                const option = document.createElement('option');
                option.value = ciudad.id_ciudad;
                option.textContent = ciudad.nombre_ciudad;
                selectCiudad.appendChild(option);
            });
            selectCiudad.disabled = false;
        } catch (error) {
            mostrarMensaje('Error al cargar ciudades: ' + error.message, 'danger');
        }
    }

    // --- 5. Función para mostrar mensajes ---
    function mostrarMensaje(mensaje, tipo) {
        mensajeArea.textContent = mensaje;
        mensajeArea.className = `alert alert-${tipo}`;
        if (tipo === 'success') mensajeArea.textContent = '✅ ' + mensaje;
        else if (tipo === 'warning') mensajeArea.textContent = '⚠️ ' + mensaje;
        else if (tipo === 'danger') mensajeArea.textContent = '❌ ' + mensaje;
        mensajeArea.classList.remove('d-none');
        setTimeout(() => {
            mensajeArea.classList.add('d-none');
            mensajeArea.className = '';
        }, 5000);
    }

    // --- 6. Función para registrar Envío ---
    async function registrarEnvio(e) {
        e.preventDefault();
        btnRegistrar.disabled = true;
        btnRegistrar.textContent = 'Registrando...';
        const datosEnvio = {
            id_cliente: parseInt(selectCliente.value),
            id_ciudad: parseInt(selectCiudad.value),
            descripcion: inputDescripcion.value.trim()
        };
        try {
            const response = await fetch(`${API_URL}/envios`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(datosEnvio)
            });
            const data = await response.json();
            if (!response.ok) throw new Error(data.mensaje || 'Error desconocido');
            mostrarMensaje(data.mensaje, 'success');
            form.reset();
            selectCiudad.innerHTML = '<option value="">Seleccione un estado primero...</option>';
            selectCiudad.disabled = true;
        } catch (error) {
            mostrarMensaje(error.message, 'danger');
        } finally {
            btnRegistrar.disabled = false;
            btnRegistrar.textContent = 'Registrar Envío';
        }
    }

    // --- 7. Función para registrar Cliente ---
    async function registrarCliente(e) {
        e.preventDefault();
        const nombre = inputClienteNombre.value.trim();
        const correo = inputClienteCorreo.value.trim();
        if (!nombre || !correo) {
            mostrarMensaje('Nombre y correo son requeridos', 'danger');
            return;
        }
        try {
            const response = await fetch(`${API_URL}/clientes`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ nombre, correo })
            });
            const data = await response.json();
            if (!response.ok) throw new Error(data.mensaje);
            mostrarMensaje(data.mensaje, 'success');
            formCliente.reset();
            cargarClientes();
        } catch (error) {
            mostrarMensaje(error.message, 'danger');
        }
    }

    // --- 8. Función para eliminar Cliente ---
    async function eliminarCliente() {
        const idCliente = selectCliente.value;
        if (!idCliente) {
            mostrarMensaje('Seleccione un cliente para eliminar', 'warning');
            return;
        }
        if (!confirm(`¿Está seguro de eliminar al cliente?`)) return;
        try {
            const response = await fetch(`${API_URL}/clientes/${idCliente}`, { method: 'DELETE' });
            const data = await response.json();
            if (!response.ok) throw new Error(data.mensaje);
            mostrarMensaje(data.mensaje, 'success');
            cargarClientes();
            enviosArea.innerHTML = '';
        } catch (error) {
            mostrarMensaje(error.message, 'danger');
        }
    }

    // --- 9. Función para ver Envíos ---
    async function verEnviosCliente() {
        const idCliente = selectCliente.value;
        if (!idCliente) {
            mostrarMensaje('Seleccione un cliente para ver sus envíos', 'warning');
            return;
        }
        enviosArea.innerHTML = '<h4>Cargando envíos...</h4>';
        try {
            const response = await fetch(`${API_URL}/clientes/${idCliente}/envios`);
            if (!response.ok) throw new Error('Error al cargar envíos');
            const envios = await response.json();
            if (envios.length === 0) {
                enviosArea.innerHTML = '<h4>Este cliente no tiene envíos registrados.</h4>';
                return;
            }
            let html = '<h4>Envíos del Cliente</h4><ul class="list-group">';
            envios.forEach(envio => {
                html += `
                    <li class="list-group-item d-flex justify-content-between align-items-center">
                        <div>
                            <strong>ID: ${envio.id_envio}</strong> - ${envio.descripcion}
                            <small class="d-block">Destino: ${envio.ciudad_destino} | Estatus: ${envio.estatus}</small>
                        </div>
                        <button class="btn btn-sm btn-info btn-ver-detalle" data-id="${envio.id_envio}">Ver Detalle</button>
                    </li>`;
            });
            html += '</ul>';
            enviosArea.innerHTML = html;
        } catch (error) {
            mostrarMensaje(error.message, 'danger');
        }
    }

    // --- 10. Función para ver Detalle (Modal) ---
    async function verDetalleEnvio(idEnvio) {
        modalContenido.innerHTML = 'Cargando detalle...';
        modalDetalle.style.display = 'block';
        try {
            const response = await fetch(`${API_URL}/envios/${idEnvio}`);
            if (!response.ok) throw new Error('No se pudo cargar el detalle');
            const detalle = await response.json();
            const fecha = new Date(detalle.fecha_envio).toLocaleString('es-MX', { timeZone: 'UTC' });
            modalContenido.innerHTML = `
                <h5>Detalle del Envío #${detalle.id_envio}</h5>
                <p><strong>Descripción:</strong> ${detalle.descripcion}</p>
                <p><strong>Estatus:</strong> ${detalle.estatus}</p>
                <p><strong>Fecha:</strong> ${fecha}</p>
                <hr><h6>Cliente</h6>
                <p><strong>Nombre:</strong> ${detalle.nombre_cliente}</p>
                <p><strong>Correo:</strong> ${detalle.correo}</p>
                <hr><h6>Destino</h6>
                <p><strong>Ciudad:</strong> ${detalle.ciudad_destino}</p>
                <p><strong>Estado:</strong> ${detalle.estado_destino}</p>`;
        } catch (error) {
            modalContenido.innerHTML = `<p class="text-danger">Error: ${error.message}</p>`;
        }
    }

    // =========================================
    // --- 11. EVENT LISTENERS (¡ESTO FALTABA!) ---
    // =========================================

    selectEstado.addEventListener('change', () => cargarCiudades(selectEstado.value));
    form.addEventListener('submit', registrarEnvio);

    if (formCliente) formCliente.addEventListener('submit', registrarCliente);
    if (btnVerEnvios) btnVerEnvios.addEventListener('click', verEnviosCliente);
    if (btnEliminarCliente) btnEliminarCliente.addEventListener('click', eliminarCliente);

    // Listener para botones dinámicos (Ver Detalle)
    enviosArea.addEventListener('click', (e) => {
        if (e.target.classList.contains('btn-ver-detalle')) {
            verDetalleEnvio(e.target.dataset.id);
        }
    });

    // Listeners del Modal
    if (modalCerrar) modalCerrar.addEventListener('click', () => modalDetalle.style.display = 'none');
    if (modalDetalle) modalDetalle.addEventListener('click', (e) => {
        if (e.target === modalDetalle) modalDetalle.style.display = 'none';
    });

    // --- 12. Carga inicial ---
    cargarEstados();
    cargarClientes();

});