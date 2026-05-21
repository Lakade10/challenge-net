<template>
  <div class="card" style="max-width: 560px">
    <h2>Nuevo turno</h2>
    <form @submit.prevent="guardar">
      <div class="form-group">
        <label>Paciente</label>
        <select v-model="form.pacienteId" required>
          <option value="">Seleccioná un paciente</option>
          <option v-for="p in pacientes" :key="p.id" :value="p.id">
            {{ p.nombreCompleto }} — DNI {{ p.dni }}
          </option>
        </select>
      </div>
      <div class="form-group">
        <label>Médico</label>
        <select v-model="form.medicoId" required>
          <option value="">Seleccioná un médico</option>
          <option v-for="m in medicos" :key="m.id" :value="m.id">
            {{ m.nombreCompleto }} — {{ m.especialidad }}
          </option>
        </select>
      </div>
      <div class="form-group">
        <label>Fecha y hora</label>
        <input type="datetime-local" :min="fechaMinima" v-model="form.fechaHora" />
      </div>
      <div class="form-group">
        <label>Motivo</label>
        <input type="text" v-model="form.motivo" maxLength="100" placeholder="Motivo de la consulta" />
      </div>
      <button type="submit" :disabled="isLoading">Confirmar turno</button>
    </form>
  </div>
</template>

<script>
import { turnosApi, pacientesApi, medicosApi } from '../services/api'

export default {
  name: 'TurnoNuevo',
  data() {
    return {
      form: {
        pacienteId: '',
        medicoId: '',
        fechaHora: '',
        motivo: ''
      },
      pacientes: [],
      medicos: [],
      isLoading: false
    }
  },
  async mounted() {
    try {
      const [pRes, mRes] = await Promise.all([pacientesApi.getAll(), medicosApi.getAll()])
      this.pacientes = pRes.data
      this.medicos = mRes.data
    } catch (error) {
      alert(error.response?.data?.mensaje || 'Error al cargar los datos iniciales')
    }
  },
  computed: {
    fechaMinima() {
      const hoy = new Date();
      hoy.setMinutes(hoy.getMinutes() - hoy.getTimezoneOffset());
      console.log(hoy.toISOString().slice(0, 16))
      return hoy.toISOString().slice(0, 16);
    }
  },
  methods: {
    async guardar() {
      try {
        this.isLoading = true
        await turnosApi.create({
          pacienteId: Number(this.form.pacienteId),
          medicoId: Number(this.form.medicoId),
          fechaHora: this.form.fechaHora,
          motivo: this.form.motivo
        })
        this.$router.push('/turnos')
      } catch (error) {
        alert(error.response?.data?.mensaje || 'Error al crear el turno')
      } finally {
        this.isLoading = false
      }
    }
  }
}
</script>
<style scoped>
  button:disabled {
    background-color: #e0e0e0;
    color: #9e9e9e;
    cursor: not-allowed;
    opacity: 0.8;
    border: 1px solid #cccccc;
  }
</style>